using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Kafka;

using Orleans;

namespace NuClear.Broadway.Grains
{
    public class StateModificationCallFilter : IIncomingGrainCallFilter
    {
        private readonly ILogger<StateModificationCallFilter> _logger;
        private readonly MessageSender _messageSender;

        public StateModificationCallFilter(ILogger<StateModificationCallFilter> logger, MessageSender messageSender)
        {
            _logger = logger;
            _messageSender = messageSender;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var attribute = context.ImplementationMethod.GetCustomAttribute<StateModificationAttribute>();
            if (attribute != null)
            {
                if (context.Grain is IVersionedGrain versionedGrain)
                {
                    var grainType = context.ImplementationMethod.DeclaringType.FullName;
                    var grainKey = context.Grain.GetPrimaryKeyLong().ToString();
                    var grainVersion = versionedGrain.GetCurrentVersion();

                    var message = JsonConvert.SerializeObject(new { grainType, grainKey, grainVersion });

                    await _messageSender.SendAsync("topic", grainKey, message);

                    _logger.LogInformation(
                        "State of {grainType}:{grainKey}, version = {version} modified.",
                        grainType,
                        grainKey,
                        grainVersion);
                }
            }

            await context.Invoke();
        }
    }
}