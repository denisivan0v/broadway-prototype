using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ISecondRubricGrain : IGrainWithIntegerKey
    {
        Task AddRubricAsync(long rubricCode);
        Task RemoveRubricAsync(long rubricCode);
        Task UpdateStateAsync(SecondRubric secondRubric);
        Task DeleteAsync();
    }
}