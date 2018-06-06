using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;

using Orleans;
using Orleans.Concurrency;

using Polly;
using Polly.Retry;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker]
    public sealed class DataProjectionDispatchingGrain : FlowConsumerGrain, IDataProjectionDispatchingGrain
    {
        private const string EventTypeContextKey = nameof(EventTypeContextKey);
        private const string GrainTypeContextKey = nameof(GrainTypeContextKey);
        private const string GrainKeyContextKey = nameof(GrainKeyContextKey);
        private const string GrainVersionContextKey = nameof(GrainVersionContextKey);

        private const string ConsumerGroupToken = "roads-state-events-consumer";
        private const string Topic = "roads_test_state_events";

        private static readonly Dictionary<string, Func<IGrainFactory, long, IGrain>> GrainProviders =
            new Dictionary<string, Func<IGrainFactory, long, IGrain>>
                {
                    { typeof(CategoryGrain).FullName, (factory, id) => factory.GetGrain<ICategoryGrain>(id) },
                    { typeof(SecondRubricGrain).FullName, (factory, id) => factory.GetGrain<ISecondRubricGrain>(id) },
                    { typeof(RubricGrain).FullName, (factory, id) => factory.GetGrain<IRubricGrain>(id) },
                    { typeof(FirmGrain).FullName, (factory, id) => factory.GetGrain<IFirmGrain>(id) },
                    { typeof(CardForERMGrain).FullName, (factory, id) => factory.GetGrain<ICardForERMGrain>(id) }
                };

        private readonly RetryPolicy<bool> _waitPolicy;

        public DataProjectionDispatchingGrain(ILogger<DataProjectionDispatchingGrain> logger, KafkaOptions kafkaOptions)
            : base(logger, kafkaOptions, ConsumerGroupToken, Topic)
        {
            _waitPolicy =
                Policy.HandleResult(true)
                      .WaitAndRetryForeverAsync(
                          (attempt, context) => TimeSpan.FromSeconds(1),
                          (result, duration, context) =>
                              {
                                  logger.LogWarning(
                                      "Race condition: {eventType} has arrived earlier than grain state was actually modified. " +
                                      "This check will be retried until grain version increase. " +
                                      "Grain: {grainType}:{grainKey}. Grain version: {grainVersion}.",
                                      context[EventTypeContextKey],
                                      context[GrainTypeContextKey],
                                      context[GrainKeyContextKey],
                                      context[GrainVersionContextKey]);
                              });
        }

        protected override async Task ProcessMessage(Message<string, string> message)
        {
            var @event = GrainStateModifyingEvent.Deserialize(message.Value);
            if (GrainProviders.TryGetValue(@event.GrainType, out var provider))
            {
                var grain = (IStateProjectorGrain)provider(GrainFactory, @event.GrainKey);

                await _waitPolicy.ExecuteAsync(
                    async (cancellationToken, context) =>
                        {
                            var currentVersion = await grain.GetCurrentVersionAsync();

                            return currentVersion == @event.GrainVersion;
                        },
                    new Dictionary<string, object>
                        {
                            { EventTypeContextKey, nameof(GrainStateModifyingEvent) },
                            { GrainTypeContextKey, @event.GrainType },
                            { GrainKeyContextKey, @event.GrainKey },
                            { GrainVersionContextKey, @event.GrainVersion },
                        },
                    CancellationToken.None,
                    true);

                await grain.ProjectStateAsync();
            }
            else
            {
                throw new ApplicationException(
                    $"Unknown grain type {@event.GrainType} used in {nameof(GrainStateModifyingEvent)}. " +
                    $"Please check {nameof(DataProjectionDispatchingGrain)} configuration.");
            }
        }
    }
}