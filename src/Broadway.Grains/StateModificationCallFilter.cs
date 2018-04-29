using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
                _logger.LogInformation(
                    "State of {grainType}:{grainKey} ({identity}) modified.",
                    context.ImplementationMethod.DeclaringType.Name,
                    context.Grain.GetPrimaryKeyLong(),
                    context.Grain.ToString());
            }
        }
    }
}