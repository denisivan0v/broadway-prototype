using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ICategoryGrain : IGrainWithIntegerKey
    {
        Task AddSecondRubric(long secondRubricCode);
        Task RemoveSecondRubric(long secondRubricCode);
        Task UpdateStateAsync(Category category);
    }
}