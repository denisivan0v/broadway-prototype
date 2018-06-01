using System;
using System.Threading;
using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Workers;

using Orleans;
using Orleans.Runtime;

namespace NuClear.Broadway.Silo.StartupTasks
{
    public class RunFlowConsumersStartupTask : IStartupTask
    {
        private readonly IGrainFactory _grainFactory;

        public RunFlowConsumersStartupTask(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            var tcs = new GrainCancellationTokenSource();
            cancellationToken.Register(() => tcs.Cancel());

            var flowKaleidoscopeConsumerGrain = _grainFactory.GetGrain<IFlowKaleidoscopeConsumerGrain>(Guid.NewGuid().ToString());
            var dataProjectionGrain = _grainFactory.GetGrain<IDataProjectionDispatchingGrain>(Guid.NewGuid().ToString());

            await flowKaleidoscopeConsumerGrain.StartExecutingAsync(tcs.Token);
            await dataProjectionGrain.StartExecutingAsync(tcs.Token);
        }
    }
}