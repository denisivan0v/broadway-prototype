using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface ISecondRubricGrain : IStateProjectorGrain
    {
        Task AddRubricAsync(long rubricCode);
        Task RemoveRubricAsync(long rubricCode);
        Task UpdateStateAsync(SecondRubric secondRubric);
        Task DeleteAsync(long code);
    }
}