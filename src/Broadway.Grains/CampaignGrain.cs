using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Interfaces.Events;
using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class CampaignGrain : JournaledGrain<Campaign>, ICampaignGrain
    {
        private readonly ILogger<CampaignGrain> _logger;

        public CampaignGrain(ILogger<CampaignGrain> logger)
        {
            _logger = logger;
        }
        
        public Task<Campaign> GetStateAsync() => Task.FromResult(State);
        
        public async Task<IReadOnlyList<object>> GetConfirmedEvents() => await RetrieveConfirmedEvents(0, Version);

        public async Task ChangeNameAsync(string name, long userId)
        {
            RaiseEvent(new CampaignNameChangedEvent {Name = name, ChangedAt = DateTime.UtcNow, ChangedBy = userId});
            await ConfirmEvents();
        }

        public async Task Start(long userId)
        {
            RaiseEvent(new CampaignStartedEvent {StartedAt = DateTime.UtcNow, UserId = userId});
            await ConfirmEvents();
        }

        public async Task Pause(long userId)
        {
            RaiseEvent(new CampaignPausedEvent {PausedAt = DateTime.UtcNow, UserId = userId});
            await ConfirmEvents();
        }

        protected override void OnStateChanged()
        {
            base.OnStateChanged();
            _logger.LogInformation("State changed. Current version is {version}", Version);
        }
    }
}