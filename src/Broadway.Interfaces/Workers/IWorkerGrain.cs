using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces.Workers
{
    public interface IWorkerGrain : IGrainWithIntegerKey
    {
        Task Execute(GrainCancellationToken cancellation);
    }
}