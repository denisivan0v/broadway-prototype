using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface IRubricGrain : IStateProjectorGrain
    {
        Task UpdateStateAsync(Rubric rubric);
        Task DeleteAsync(long code);
    }
}