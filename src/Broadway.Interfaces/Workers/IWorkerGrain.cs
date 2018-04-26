using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces.Workers
{
    public interface IWorkerGrain : IGrainWithStringKey
    {
        Task StartExecutingAsync(GrainCancellationToken cancellation);
    }
}