using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace NuClear.Broadway.Kafka
{
    public sealed class MessageReceiver : ConsumerWrapper
    {
        private readonly IEnumerable<string> _topics;
        
        private bool _streamingStarted;

        public MessageReceiver(ILogger logger, KafkaOptions kafkaOptions, string groupId, IEnumerable<string> topics)
            : base(logger, kafkaOptions, groupId)
        {
            _topics = topics;
        }
        
        public IObservable<KafkaMessage> Subscribe(CancellationToken cancellationToken)
        {
            if (_streamingStarted)
            {
                throw new InvalidOperationException("Streaming already started. Please dispose the previous obvservable before getting the new one.");
            }

            var pollCancellationTokenSource = new CancellationTokenSource();
            var registration = cancellationToken.Register(() => pollCancellationTokenSource.Cancel());

            var onMessage = Observable.FromEventPattern<Message<string, string>>(
                                          x =>
                                              {
                                                  Consumer.OnMessage += x;
                                                  Consumer.Subscribe(_topics);
                                              },
                                          x =>
                                              {
                                                  pollCancellationTokenSource.Cancel();
                                                  registration.Dispose();
                                                  Consumer.Unsubscribe();
                                                  Consumer.OnMessage -= x;
                                                  _streamingStarted = false;
                                              })
                                      .Select(x => new KafkaMessage(
                                                      x.EventArgs.Key, 
                                                      x.EventArgs.Value, 
                                                      x.EventArgs.TopicPartitionOffset, 
                                                      x.EventArgs.Timestamp));
            Task.Factory.StartNew(
                    () =>
                        {
                            while (!pollCancellationTokenSource.IsCancellationRequested)
                            {
                                Consumer.Poll(TimeSpan.FromMilliseconds(100));
                            }
                        },
                    pollCancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Current);

            _streamingStarted = true;

            return onMessage;
        }

        public async Task CommitAsync<TSourceEvent>(KafkaMessage message)
        {
            var offsetsToCommit = new TopicPartitionOffset(message.TopicPartitionOffset.TopicPartition, message.TopicPartitionOffset.Offset + 1);
            await Consumer.CommitAsync(new[] { offsetsToCommit });
        }
    }
}