using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

using Orleans;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface ICategoryGrain : IGrainWithIntegerKey
    {
        Task AddSecondRubricAsync(long secondRubricCode);
        Task RemoveSecondRubricAsync(long secondRubricCode);
        Task UpdateStateAsync(Category category);
        Task DeleteAsync(long code);
    }
}