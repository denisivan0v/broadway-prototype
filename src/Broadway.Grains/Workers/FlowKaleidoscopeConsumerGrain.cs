using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        private const string ConsumerGroupId = "roads-flow-kaleidoscope-consumer";
        
        private readonly ILogger<FlowKaleidoscopeConsumerGrain> _logger;
        private readonly MessageReceiver _messageReceiver;

        private CancellationTokenRegistration _cancellationTokenRegistration;
        
        public FlowKaleidoscopeConsumerGrain(
            ILogger<FlowKaleidoscopeConsumerGrain> logger,
            KafkaOptions kafkaOptions)
        {
            _logger = logger;
            _messageReceiver = new MessageReceiver(
                logger,
                kafkaOptions,
                $"{ConsumerGroupId}-{kafkaOptions.ConsumerGroupToken}",
                new[] {"casino_staging_flowKaleidoscope_compacted"});
        }
        
        public Task Execute(GrainCancellationToken cancellation)
        {
            var (observable, poll) = _messageReceiver.Subscribe(cancellation.CancellationToken);

            var subscription = observable
                .Subscribe(async message =>
                {
                    _logger.LogInformation("Message received.");
                    
                    var xml = XElement.Parse(message.Source);
                    await UpdateCategoryAsync(xml);
                });

            _cancellationTokenRegistration = cancellation.CancellationToken.Register(() => subscription.Dispose());

            while (!cancellation.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Poll for the next message.");
                poll();
            }
            
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
            _cancellationTokenRegistration.Dispose();
            _messageReceiver.Dispose();
        }

        public async Task UpdateCategoryAsync(XElement xml)
        {
            if (xml.Attribute(nameof(Category.Code)) == null || xml.Attribute(nameof(Category.IsDeleted)) == null)
            {
                return;
            }
            
            var category = new Category
            {
                Code = (long) xml.Attribute(nameof(Category.Code)),
                IsDeleted = (bool) xml.Attribute(nameof(Category.IsDeleted))
            };
            
            var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(category.Code);
            
            _logger.LogInformation("Going to update state...");
            await categoryGrain.UpdateStateAsync(category);
        }
    }
}