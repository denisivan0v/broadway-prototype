using System.Threading.Tasks;

using Orleans;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface IStateProjectorGrain : IGrainWithIntegerKey
    {
        Task<int> GetCurrentVersionAsync();
        Task ProjectStateAsync();
    }
}