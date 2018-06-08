using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface ICategoryGrain : IStateProjectorGrain
    {
        Task AddSecondRubricAsync(long secondRubricCode);
        Task RemoveSecondRubricAsync(long secondRubricCode);
        Task UpdateStateAsync(Category category);
        Task DeleteAsync(long code);
    }
}