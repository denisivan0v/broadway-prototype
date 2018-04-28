using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface IFirmGrain : IGrainWithIntegerKey
    {
        Task AddCardAsync(long cardCode);
        Task RemoveCardAsync(long cardCode);
        Task Archive();
        Task UpdateStateAsync(Firm firm);
    }
}