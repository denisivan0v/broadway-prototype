using System.Reflection;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Kafka;

using Orleans;

namespace NuClear.Broadway.Grains
{
    public class StateModificationCallFilter : IIncomingGrainCallFilter
    {
        private const string Topic = "roads_test_state_events";

        private readonly MessageSender _messageSender;

        public StateModificationCallFilter(MessageSender messageSender)
        {
            _messageSender = messageSender;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var attribute = context.ImplementationMethod.GetCustomAttribute<StateModificationAttribute>();
            if (attribute != null)
            {
                if (context.Grain is IStateProjectorGrain stateProjectorGrain)
                {
                    var grainType = context.ImplementationMethod.DeclaringType.FullName;
                    var grainKey = context.Grain.GetPrimaryKeyLong();
                    var grainVersion = await stateProjectorGrain.GetCurrentVersionAsync();

                    var @event = new GrainStateModifyingEvent(grainType, grainKey, grainVersion);
                    var message = @event.Serialize();

                    var messageKey = $"{grainType}|{grainKey}";
                    await _messageSender.SendAsync(Topic, messageKey, message);
                }
            }

            await context.Invoke();
        }
    }
}