using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ICategoryGrain : IGrainWithIntegerKey
    {
        Task AddSecondRubricAsync(long secondRubricCode);
        Task RemoveSecondRubricAsync(long secondRubricCode);
        Task UpdateStateAsync(Category category);
        Task DeleteAsync(long code);
    }
}