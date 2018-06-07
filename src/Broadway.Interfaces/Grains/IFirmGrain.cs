using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

using Orleans;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface IFirmGrain : IStateProjectorGrain
    {
        Task AddCardAsync(long cardCode);
        Task RemoveCardAsync(long cardCode);
        Task Archive(int branchCode, int? countryCode);
        Task UpdateStateAsync(Firm firm);
    }
}