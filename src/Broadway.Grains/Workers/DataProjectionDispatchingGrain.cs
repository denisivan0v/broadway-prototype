using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;

using Orleans;
using Orleans.Concurrency;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker]
    public sealed class DataProjectionDispatchingGrain : FlowConsumerGrain, IDataProjectionDispatchingGrain
    {
        private const string ConsumerGroupToken = "roads-state-events-consumer";
        private const string Topic = "roads_test_state_events";

        private static readonly Dictionary<string, Func<IGrainFactory, long, IGrain>> GrainProviders =
            new Dictionary<string, Func<IGrainFactory, long, IGrain>>
                {
                    { typeof(CategoryGrain).FullName, (factory, id) => factory.GetGrain<ICategoryGrain>(id) },
                    { typeof(SecondRubricGrain).FullName, (factory, id) => factory.GetGrain<ISecondRubricGrain>(id) },
                    { typeof(RubricGrain).FullName, (factory, id) => factory.GetGrain<IRubricGrain>(id) }
                };

        private readonly ILogger<DataProjectionDispatchingGrain> _logger;

        public DataProjectionDispatchingGrain(ILogger<DataProjectionDispatchingGrain> logger, KafkaOptions kafkaOptions)
            : base(logger, kafkaOptions, ConsumerGroupToken, Topic)
        {
            _logger = logger;
        }

        protected override async Task ProcessMessage(Message<string, string> message)
        {
            var @event = GrainStateModifyingEvent.Deserialize(message.Value);
            if (GrainProviders.TryGetValue(@event.GrainType, out var provider))
            {
                var grain = (IStateProjectorGrain)provider(GrainFactory, @event.GrainKey);

                var currentVersion = await grain.GetCurrentVersionAsync();
                while (currentVersion == @event.GrainVersion)
                {
                    _logger.LogWarning(
                        "Race condition: {eventType} has arrived earlier than grain state was actually modified. " +
                        "This check will be retried until grain version increase. " +
                        "Grain: {grainType}:{grainKey}. Grain version: {grainVersion}.",
                        nameof(GrainStateModifyingEvent),
                        @event.GrainType,
                        @event.GrainKey,
                        @event.GrainVersion);

                    await Task.Delay(1000);

                    currentVersion = await grain.GetCurrentVersionAsync();
                }

                await grain.ProjectStateAsync();
            }
        }
    }
}