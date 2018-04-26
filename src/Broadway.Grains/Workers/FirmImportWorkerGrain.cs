using System.Threading.Tasks;
using NuClear.Broadway.Interfaces.Workers;
using Orleans;
using Orleans.Concurrency;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker]
    public class FirmImportWorkerGrain : Grain, IFirmImportWorkerGrain
    {
        public async Task StartExecutingAsync(GrainCancellationToken cancellation)
        {
            return;
        }
    }
}