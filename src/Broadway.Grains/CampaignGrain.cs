using System;
using System.Threading.Tasks;
using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Interfaces.Events;
using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class CampaignGrain : JournaledGrain<Campaign>, ICampaignGrain
    {
        public Task<Campaign> GetState() => Task.FromResult(State);
        
        public Task ChangeName(string name, long userId)
        {
            RaiseEvent(new CampaignNameChangedEvent {Name = name, ChangedAt = DateTime.UtcNow, ChangedBy = userId});
            return Task.CompletedTask;
        }

        public Task Start(long userId)
        {
            RaiseEvent(new CampaignStartedEvent {StartedAt = DateTime.UtcNow, UserId = userId});
            return Task.CompletedTask;
        }

        public Task Pause(long userId)
        {
            RaiseEvent(new CampaignPausedEvent {PausedAt = DateTime.UtcNow, UserId = userId});
            return Task.CompletedTask;
        }

        protected override void OnStateChanged()
        {
            base.OnStateChanged();
            
            // Do logging here...
        }
    }
}