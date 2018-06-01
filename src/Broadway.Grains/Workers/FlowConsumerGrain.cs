using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;
using Orleans;

namespace NuClear.Broadway.Grains.Workers
{
    public abstract class FlowConsumerGrain : Grain, IWorkerGrain
    {
        private const int BufferSize = 100;

        private readonly ILogger _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly string _consumerGroupToken;
        private readonly string _topic;

        private readonly BufferBlock<Message<string, string>> _messageProcessingQueue =
            new BufferBlock<Message<string, string>>();

        private SimpleMessageReceiver _messageReceiver;

        protected FlowConsumerGrain(ILogger logger, KafkaOptions kafkaOptions, string consumerGroupToken, string topic)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions;
            _consumerGroupToken = consumerGroupToken;
            _topic = topic;
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            _messageReceiver = new SimpleMessageReceiver(
                _logger,
                _kafkaOptions,
                $"{_consumerGroupToken}-{_kafkaOptions.ConsumerGroupPostfix}",
                new[] { _topic });

            _messageReceiver.OnMessage += OnMessage;

            _logger.LogTrace("{grainType} has beed activated for topic {topic}.", GetType().Name, _topic);
        }

        public override Task OnDeactivateAsync()
        {
            _messageReceiver.OnMessage -= OnMessage;
            _messageReceiver.Dispose();

            var task = base.OnDeactivateAsync();

            _logger.LogTrace("{grainType} has beed deactivated.", GetType().Name);

            return task;
        }

        public Task StartExecutingAsync(GrainCancellationToken cancellation)
        {
            var waitHandle = new ManualResetEventSlim(false);

            RunPollLoopOnThreadpool(cancellation.CancellationToken, waitHandle);

            RunMessageProcessingLoop(cancellation.CancellationToken, waitHandle);

            return Task.CompletedTask;
        }

        protected abstract Task ProcessMessage(Message<string, string> message);

        private void OnMessage(object sender, Message<string, string> message) => _messageProcessingQueue.Post(message);

        private void RunPollLoopOnThreadpool(CancellationToken cancellationToken, ManualResetEventSlim waitHandle)
        {
            Task.Run(
                () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            if (_messageProcessingQueue.Count >= BufferSize)
                            {
                                _logger.LogTrace("Enabling consumer backpressure...");
                                waitHandle.Reset();
                                waitHandle.Wait(cancellationToken);
                            }

                            _messageReceiver.Poll();
                        }
                    },
                cancellationToken);

            _logger.LogTrace("Kafka poll loop started.");
        }

        private void RunMessageProcessingLoop(CancellationToken cancellationToken, ManualResetEventSlim waitHandle)
        {
            var scheduler = TaskScheduler.Current;
            Task.Factory.StartNew(
                async () =>
                    {
                        while (await _messageProcessingQueue.OutputAvailableAsync(cancellationToken))
                        {
                            var message = await _messageProcessingQueue.ReceiveAsync(cancellationToken);
                            _logger.LogTrace("Got new message from Kafka.");

                            if (_messageProcessingQueue.Count < BufferSize && !waitHandle.IsSet)
                            {
                                waitHandle.Set();
                                _logger.LogTrace("Consumer backpressure disabled.");
                            }

                            await ProcessMessage(message);

                            await _messageReceiver.CommitAsync(message);

                            _logger.LogTrace("A message from Kafka processed successfully.");
                        }
                    },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                scheduler);
        }
    }
}