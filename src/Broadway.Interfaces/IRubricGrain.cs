using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface IRubricGrain : IGrainWithIntegerKey
    {
        Task UpdateStateAsync(Rubric rubric);
        Task DeleteAsync(long code);
    }
}