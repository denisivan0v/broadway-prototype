using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

using Orleans;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface ICardForERMGrain : IGrainWithIntegerKey
    {
        Task<long> GetFirmCodeAsync();
        Task UpdateStateAsync(CardForERM cardForErm);
    }
}