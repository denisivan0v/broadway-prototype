using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ICategoryGrain : IGrainWithIntegerKey
    {
        Task UpdateStateAsync(Category category);
    }
}