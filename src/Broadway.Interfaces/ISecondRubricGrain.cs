using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ISecondRubricGrain : IGrainWithIntegerKey
    {
        Task AddRubric(long rubricCode);
        Task RemoveRubric(long rubricCode);
        Task UpdateStateAsync(SecondRubric secondRubric);
    }
}