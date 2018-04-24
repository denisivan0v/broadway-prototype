using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
        
        public async Task Execute(GrainCancellationToken cancellation)
        {
            var subscription = Subscribe(cancellation.CancellationToken);
            
            var taskCompletionSource = new TaskCompletionSource<object>();
            _cancellationTokenRegistration = cancellation.CancellationToken.Register(
                () =>
                {
                    subscription.Dispose();
                    taskCompletionSource.SetResult(null);
                });
            await taskCompletionSource.Task;
        }
        
        public void Dispose()
        {
            _cancellationTokenRegistration.Dispose();
            _messageReceiver.Dispose();
        }

        private IDisposable Subscribe(CancellationToken cancellationToken)
        {
            var observable = _messageReceiver.Subscribe(cancellationToken);

            return observable
                .Subscribe(async message =>
                {
                    var xml = XElement.Parse(message.Source);
                    await ImportCategoryAsync(xml);
                });
        }

        public async Task ImportCategoryAsync(XElement xml)
        {
            var category = new Category
            {
                Code = (int) xml.Attribute(nameof(Category.Code)),
                IsDeleted = (bool) xml.Attribute(nameof(Category.IsDeleted))
            };
            
            var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(category.Code);
            await categoryGrain.UpdateStateAsync(category);
        }
    }
}