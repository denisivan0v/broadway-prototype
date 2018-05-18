using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces;

using Orleans;

namespace NuClear.Broadway.Grains
{
    public class StateModificationCallFilter : IIncomingGrainCallFilter
    {
        private readonly ILogger<StateModificationCallFilter> _logger;

        public StateModificationCallFilter(ILogger<StateModificationCallFilter> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            await context.Invoke();

            var attribute = context.ImplementationMethod.GetCustomAttribute<StateModificationAttribute>();
            if (attribute != null)
            {
                if (context.Grain is IVersionedGrain versionedGrain)
                {
                    _logger.LogInformation(
                        "State of {grainType}:{grainKey}, version = {version} modified.",
                        context.ImplementationMethod.DeclaringType.FullName,
                        context.Grain.GetPrimaryKeyLong(),
                        versionedGrain.GetCurrentVersion());
                }
            }
        }
    }
}