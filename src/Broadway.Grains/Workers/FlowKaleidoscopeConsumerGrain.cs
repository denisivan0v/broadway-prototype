using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
        private readonly KafkaOptions _kafkaOptions;
        
        private  MessageReceiver _messageReceiver;
        private TaskPoolScheduler _rxScheduler;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        
        public FlowKaleidoscopeConsumerGrain(
            ILogger<FlowKaleidoscopeConsumerGrain> logger,
            KafkaOptions kafkaOptions)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions;
        }
        
        public async Task Execute(GrainCancellationToken cancellation)
        {
            var (observable, poll) = _messageReceiver.Subscribe(cancellation.CancellationToken);

//            var subscription = observable
//                .ObserveOn(_rxScheduler)
//                .SelectMany(async message =>
//                {
//                    var xml = XElement.Parse(message.Source);
//                    return await UpdateCategoryAsync(xml);
//                })
//                .Subscribe();

            var orleansScheduler = TaskScheduler.Current;
            var subscription = observable
                .SelectMany(message =>
                    Task.Factory.StartNew(async () =>
                            {
                                var xml = XElement.Parse(message.Source);
                                return await UpdateCategoryAsync(xml);
                            },
                            cancellation.CancellationToken,
                            TaskCreationOptions.None,
                            orleansScheduler)
                        .Unwrap()
                        .ToObservable())
                .Publish()
                .Connect();

            _cancellationTokenRegistration = cancellation.CancellationToken.Register(() => subscription.Dispose());
            
            await RunPollLoop(cancellation.CancellationToken, poll);
        }
        
        public void Dispose()
        {
            _cancellationTokenRegistration.Dispose();
            _messageReceiver.Dispose();
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            
            _messageReceiver = new MessageReceiver(
                _logger,
                _kafkaOptions,
                $"{ConsumerGroupId}-{_kafkaOptions.ConsumerGroupToken}",
                new[] {"casino_staging_flowKaleidoscope_compacted"});

            var factory = new TaskFactory(TaskScheduler.Current);
            _rxScheduler = new TaskPoolScheduler(factory);
        }

        private Task RunPollLoop(CancellationToken cancellationToken, Action poll)
        {
            return Task.Factory.StartNew(() =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        //_logger.LogInformation("Poll for the next message.");
                        poll();
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
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
            
            //_logger.LogInformation("Going to update state...");
            await categoryGrain.UpdateStateAsync(category);

            return category;
        }
    }
}