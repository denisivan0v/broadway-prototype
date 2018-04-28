using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ICardForERMGrain : IGrainWithIntegerKey
    {
        Task<long> GetFirmCodeAsync();
        Task UpdateStateAsync(CardForERM cardForErm);
    }
}