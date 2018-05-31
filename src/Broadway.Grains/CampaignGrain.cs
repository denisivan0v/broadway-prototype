using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans.EventSourcing;
using Orleans.Providers;

namespace NuClear.Broadway.Grains
{
    [LogConsistencyProvider(ProviderName = "LogStorage")]
    public class CampaignGrain : JournaledGrain<Campaign>, ICampaignGrain, IStateProjectorGrain
    {
        private readonly ILogger<CampaignGrain> _logger;

        public CampaignGrain(ILogger<CampaignGrain> logger)
        {
            _logger = logger;
        }

        public Task<int> GetCurrentVersionAsync() => Task.FromResult(Version);

        public Task ProjectStateAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Campaign> GetStateAsync() => Task.FromResult(State);

        public async Task<IReadOnlyList<object>> GetConfirmedEvents() => await RetrieveConfirmedEvents(0, Version);

        [StateModification]
        public async Task ChangeNameAsync(string name, long userId)
        {
            RaiseEvent(new CampaignNameChangedEvent { Name = name, ChangedAt = DateTime.UtcNow, ChangedBy = userId });
            await ConfirmEvents();
        }

        public async Task Start(long userId)
        {
            RaiseEvent(new CampaignStartedEvent { StartedAt = DateTime.UtcNow, UserId = userId });
            await ConfirmEvents();
        }

        public async Task Pause(long userId)
        {
            RaiseEvent(new CampaignPausedEvent { PausedAt = DateTime.UtcNow, UserId = userId });
            await ConfirmEvents();
        }

        protected override void TransitionState(Campaign state, object @event)
        {
            switch (@event)
            {
                case CampaignNameChangedEvent nameChangedEvent:
                    state.Name = nameChangedEvent.Name;
                    break;
                case CampaignStartedEvent startedEvent:
                    state.StartedAt = startedEvent.StartedAt;
                    state.StartedBy = startedEvent.UserId;
                    break;
                case CampaignPausedEvent pausedEvent:
                    state.PausedAt = pausedEvent.PausedAt;
                    state.PausedBy = pausedEvent.UserId;
                    break;
                default:
                    _logger.LogWarning(
                        "Got an {eventType} event, but the state wasn't updated. Current version is {version}.",
                        @event.GetType(),
                        Version);
                    return;
            }

            _logger.LogInformation("State updated based on {eventType} event.", @event.GetType());
        }
    }
}