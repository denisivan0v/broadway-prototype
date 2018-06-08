using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface IBranchGrain : IStateProjectorGrain
    {
        Task UpdateStateAsync(Branch branch);
        Task DeleteAsync(int code);
    }
}