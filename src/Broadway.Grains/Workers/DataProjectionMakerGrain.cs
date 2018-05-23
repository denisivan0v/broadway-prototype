using System.Threading.Tasks;

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.DataProjection;
using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;

using Orleans.Concurrency;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker]
    public sealed class DataProjectionMakerGrain : FlowConsumerGrain, IDataProjectionMakerGrain
    {
        private const string ConsumerGroupToken = "roads-state-events-consumer";
        private const string Topic = "roads_test_state_events";

        private readonly DataProjectionContext _db;

        public DataProjectionMakerGrain(ILogger<DataProjectionMakerGrain> logger, KafkaOptions kafkaOptions, DataProjectionContext db)
            : base(logger, kafkaOptions, ConsumerGroupToken, Topic)
        {
            _db = db;
        }

        protected override Task ProcessMessage(Message<string, string> message)
        {
            throw new System.NotImplementedException();
        }
    }
}