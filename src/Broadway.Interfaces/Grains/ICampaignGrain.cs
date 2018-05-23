using System.Collections.Generic;
using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Models;

using Orleans;

namespace NuClear.Broadway.Interfaces.Grains
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