﻿using System.Threading;
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
        private readonly BufferBlock<Message<string, string>> _messageProcessingQueue = new BufferBlock<Message<string, string>>();
        
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
                $"{_consumerGroupToken}-{_kafkaOptions.ConsumerGroupToken}",
                new[] {_topic});

            _messageReceiver.OnMessage += OnMessage;
        }

        public override Task OnDeactivateAsync()
        {
            _messageReceiver.OnMessage -= OnMessage;
            _messageReceiver.Dispose();
            
            return base.OnDeactivateAsync();
        }

        public async Task StartExecutingAsync(GrainCancellationToken cancellation)
        {
            var waitHandler = new ManualResetEventSlim(false);

            var consumingTask = Task.Run(() =>
            {
                while (!cancellation.CancellationToken.IsCancellationRequested)
                {
                    if (_messageProcessingQueue.Count >= BufferSize)
                    {
                        _logger.LogDebug("Enabling consumer backpressure...");
                        waitHandler.Reset();
                        waitHandler.Wait();
                    }

                    _messageReceiver.Poll();
                }
            });

            while (await _messageProcessingQueue.OutputAvailableAsync(cancellation.CancellationToken))
            {
                var message = await _messageProcessingQueue.ReceiveAsync();
                if (_messageProcessingQueue.Count < BufferSize && !waitHandler.IsSet)
                {
                    waitHandler.Set();
                    _logger.LogDebug("Consumer backpressure disabled.");
                }

                await ProcessMessage(message);
                //await _messageReceiver.CommitAsync(message);
            }
        }

        protected abstract Task ProcessMessage(Message<string, string> message);
        
        private void OnMessage(object sender, Message<string, string> message) => _messageProcessingQueue.Post(message);
    }
}