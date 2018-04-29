using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace NuClear.Broadway.Kafka
{
    public sealed class SimpleMessageReceiver : ConsumerWrapper
    {
        public SimpleMessageReceiver(ILogger logger, KafkaOptions kafkaOptions, string groupId, IEnumerable<string> topics)
            : base(logger, kafkaOptions, groupId)
        {
            Consumer.Subscribe(topics);
        }

        public event EventHandler<Message<string, string>> OnMessage
        {
            add => Consumer.OnMessage += value;
            remove => Consumer.OnMessage -= value;
        }

        public void Poll()
        {
            Consumer.Poll(TimeSpan.FromMilliseconds(100));
        }

        public async Task<CommittedOffsets> CommitAsync(Message<string, string> message) =>
            await Consumer.CommitAsync(message);

        public override void Dispose()
        {
            Consumer.Unsubscribe();
            base.Dispose();
        }
    }
}