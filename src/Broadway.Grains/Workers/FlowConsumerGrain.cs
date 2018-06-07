using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;

using Orleans;
using Orleans.Runtime;

using Polly;
using Polly.Retry;

namespace NuClear.Broadway.Grains.Workers
{
    public abstract class FlowConsumerGrain : Grain, IWorkerGrain, IRemindable
    {
        private const int BufferSize = 100;

        private static readonly TimeSpan ReminderPeriod = TimeSpan.FromMinutes(1);

        private readonly ILogger _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly string _consumerGroupToken;
        private readonly string _topic;
        private readonly RetryPolicy _retryPolicy;

        private readonly BufferBlock<Message<string, string>> _messageProcessingQueue =
            new BufferBlock<Message<string, string>>();

        private SimpleMessageReceiver _messageReceiver;

        protected FlowConsumerGrain(ILogger logger, KafkaOptions kafkaOptions, string consumerGroupToken, string topic)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions;
            _consumerGroupToken = consumerGroupToken;
            _topic = topic;

            _retryPolicy =
                Policy.Handle<Exception>(ex => !(ex is ObjectDisposedException))
                      .WaitAndRetryForeverAsync(
                          attempt => TimeSpan.FromMilliseconds(1000),
                          (ex, duration) =>
                              {
                                  _logger.LogWarning(
                                      ex,
                                      "Unexpected error occured while consuming from topic {topic}",
                                      _topic);
                              });
        }

        public override async Task OnActivateAsync()
        {
            _messageReceiver = new SimpleMessageReceiver(
                _logger,
                _kafkaOptions,
                $"{_consumerGroupToken}-{_kafkaOptions.ConsumerGroupPostfix}",
                new[] { _topic });

            _messageReceiver.OnMessage += OnMessage;

            var grainType = GetType().Name;
            await RegisterOrUpdateReminder(grainType, ReminderPeriod, ReminderPeriod);

            _logger.LogInformation("Consumer grain of type {grainType} has beed activated for topic {topic}.", grainType, _topic);
        }

        public override Task OnDeactivateAsync()
        {
            _messageReceiver.OnMessage -= OnMessage;
            _messageReceiver.Dispose();

            _logger.LogInformation("Consumer grain of type {grainType} has beed deactivated.", GetType().Name);

            return Task.CompletedTask;
        }

        public Task StartExecutingAsync(GrainCancellationToken cancellation)
        {
            var waitHandle = new ManualResetEventSlim(false);

            RunPollLoopOnThreadpool(cancellation.CancellationToken, waitHandle);
            RunMessageProcessingLoop(cancellation.CancellationToken, waitHandle);

            return Task.CompletedTask;
        }

        protected abstract Task ProcessMessage(Message<string, string> message);

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
        {
            _logger.LogInformation("Reminder {reminderName} received. This will prevent consumer grain deactivation.", reminderName);
            return Task.CompletedTask;
        }

        private void OnMessage(object sender, Message<string, string> message) => _messageProcessingQueue.Post(message);

        private void RunPollLoopOnThreadpool(CancellationToken cancellationToken, ManualResetEventSlim waitHandle)
        {
            Task.Run(
                () =>
                    {
                        _retryPolicy.ExecuteAsync(
                            token =>
                                {
                                    while (!token.IsCancellationRequested)
                                    {
                                        if (_messageProcessingQueue.Count >= BufferSize)
                                        {
                                            _logger.LogTrace("Enabling consumer backpressure...");
                                            waitHandle.Reset();
                                            waitHandle.Wait(token);
                                        }

                                        _messageReceiver.Poll();
                                    }

                                    return Task.CompletedTask;
                                },
                            cancellationToken);
                    },
                cancellationToken);

            _logger.LogTrace("Kafka poll loop started.");
        }

        private void RunMessageProcessingLoop(CancellationToken cancellationToken, ManualResetEventSlim waitHandle)
        {
            var scheduler = TaskScheduler.Current;
            Task.Factory.StartNew(
                () =>
                    {
                        _retryPolicy.ExecuteAsync(
                            async token =>
                                {
                                    while (await _messageProcessingQueue.OutputAvailableAsync(token))
                                    {
                                        var message = await _messageProcessingQueue.ReceiveAsync(token);
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
                            true);
                    },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                scheduler);
        }
    }
}