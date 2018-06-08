using NuClear.Broadway.Kafka;

namespace NuClear.Broadway.Grains.Options
{
    public sealed class ReferenceObjectsClusterKafkaOptions : KafkaOptions
    {
        public string FlowKaleidoscopeTopic { get; set; }
        public string FlowGeoClassifierTopic { get; set; }
        public string FlowCardsForErmTopic { get; set; }
    }
}