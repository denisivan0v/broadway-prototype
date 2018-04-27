using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class StateModificationFilter : IIncomingGrainCallFilter
    {
        private readonly ILogger<StateModificationFilter> _logger;

        public StateModificationFilter(ILogger<StateModificationFilter> logger)
        {
            _logger = logger;
        }
        
        public async Task Invoke(IIncomingGrainCallContext context)
        {
            await context.Invoke();

            var attribute = context.ImplementationMethod.GetCustomAttribute<StateModificationAttribute>();
            if (attribute != null)
            {
                _logger.LogInformation("State modified.");
            }
        }
    }
}