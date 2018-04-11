using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ICampaignGrain : IGrainWithIntegerKey
    {
        Task<Campaign> GetStateAsync();
        Task<IReadOnlyList<object>> GetConfirmedEvents();

        Task ChangeNameAsync(string name, long userId);
        Task Start(long userId);
        Task Pause(long userId);
    }
}