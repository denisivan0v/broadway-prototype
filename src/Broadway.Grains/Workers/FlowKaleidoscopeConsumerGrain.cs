using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;
using Orleans;
using Orleans.Concurrency;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker(1)]
    public class FlowKaleidoscopeConsumerGrain : Grain, IFlowKaleidoscopeConsumerGrain, IDisposable
    {
        private const int BufferSize = 100;
        private const string ConsumerGroupId = "roads-flow-kaleidoscope-consumer";
        
        private readonly ILogger<FlowKaleidoscopeConsumerGrain> _logger;
        private readonly KafkaOptions _kafkaOptions;
        
        private SimpleMessageReceiver _messageReceiver;
        
        public FlowKaleidoscopeConsumerGrain(
            ILogger<FlowKaleidoscopeConsumerGrain> logger,
            KafkaOptions kafkaOptions)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions;
        }
        
        public async Task Execute(GrainCancellationToken cancellation)
        {
            var queue = new BufferBlock<Message<string, string>>(
                new DataflowBlockOptions
                {
                    CancellationToken = cancellation.CancellationToken,
                    EnsureOrdered = true
                });

            var waitHandler = new ManualResetEventSlim(false);

            var consumingTask = Task.Run(() =>
            {
                _messageReceiver.OnMessage += (_, message) => queue.Post(message);

                while (!cancellation.CancellationToken.IsCancellationRequested)
                {
                    if (queue.Count >= BufferSize)
                    {
                        _logger.LogDebug("Enabling consumer backpressure...");
                        waitHandler.Reset();
                        waitHandler.Wait();
                    }

                    _messageReceiver.Poll();
                }
            });

            while (await queue.OutputAvailableAsync(cancellation.CancellationToken))
            {
                var message = await queue.ReceiveAsync();
                if (queue.Count < BufferSize && !waitHandler.IsSet)
                {
                    waitHandler.Set();
                    _logger.LogDebug("Consumer backpressure disabled.");
                }

                var xml = XElement.Parse(message.Value);
                await UpdateCategoryAsync(xml);

                await _messageReceiver.CommitAsync(message);
            }

            await consumingTask;
        }
        
        public void Dispose()
        {
            _messageReceiver.Dispose();
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            
            _messageReceiver = new SimpleMessageReceiver(
                _logger,
                _kafkaOptions,
                $"{ConsumerGroupId}-{_kafkaOptions.ConsumerGroupToken}",
                new[] {"casino_staging_flowKaleidoscope_compacted"});
        }

        private async Task<Category> UpdateCategoryAsync(XElement xml)
        {
            if (xml.Attribute(nameof(Category.Code)) == null || xml.Attribute(nameof(Category.IsDeleted)) == null)
            {
                return null;
            }
            
            var category = new Category
            {
                Code = (long) xml.Attribute(nameof(Category.Code)),
                IsDeleted = (bool) xml.Attribute(nameof(Category.IsDeleted))
            };
            
            var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(category.Code);
            
            await categoryGrain.UpdateStateAsync(category);

            return category;
        }
    }
}