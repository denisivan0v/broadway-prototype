using System.Threading.Tasks;
using Orleans;

namespace NuClear.Broadway.Interfaces
{
    public interface ICampaignGrain : IGrainWithIntegerKey
    {
        Task<Campaign> GetState();

        Task ChangeName(string name, long userId);
        Task Start(long userId);
        Task Pause(long userId);
    }
}