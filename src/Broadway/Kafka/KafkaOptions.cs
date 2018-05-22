namespace NuClear.Broadway.Kafka
{
    public class KafkaOptions
    {
        public string BrokerEndpoints { get; set; }
        public string ConsumerGroupPostfix { get; set; }
        public ConsumerOptions Consumer { get; set; }
        public ProducerOptions Producer { get; set; }

        public TOptions MergeWith<TOptions>(TOptions options) where TOptions : KafkaOptions
        {
            if (!string.IsNullOrEmpty(BrokerEndpoints))
            {
                options.BrokerEndpoints = BrokerEndpoints;
            }

            if (!string.IsNullOrEmpty(ConsumerGroupPostfix))
            {
                options.ConsumerGroupPostfix = ConsumerGroupPostfix;
            }

            if (Consumer != null)
            {
                options.Consumer = Consumer;
            }

            if (Producer != null)
            {
                options.Producer = Producer;
            }

            return options;
        }

        public sealed class ConsumerOptions
        {
            public bool EnableAutoCommit { get; set; }
            public int FetchWaitMaxMs { get; set; }
            public int FetchErrorBackoffMs { get; set; }
            public int FetchMessageMaxBytes { get; set; }
            public int QueuedMinMessages { get; set; }
        }

        public sealed class ProducerOptions
        {
            public int QueueBufferingMaxMs { get; set; }
            public int QueueBufferingMaxKbytes { get; set; }
            public int BatchNumMessages { get; set; }
            public int MessageMaxBytes { get; set; }
        }
    }
}