using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ISecondRubricGrain : IGrainWithIntegerKey
    {
        Task UpdateStateAsync(SecondRubric secondRubric);
    }
}