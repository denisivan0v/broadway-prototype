using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Interfaces.Events;
using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class CampaignGrain : JournaledGrain<Campaign, string>, ICampaignGrain
    {
        private readonly ILogger<CampaignGrain> _logger;
        private readonly JsonSerializerSettings _serializerSettings;

        public CampaignGrain(ILogger<CampaignGrain> logger)
        {
            _logger = logger;
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            };
        }
        
        public Task<Campaign> GetStateAsync() => Task.FromResult(State);
        
        public async Task<IReadOnlyList<object>> GetConfirmedEvents() => await RetrieveConfirmedEvents(0, Version);

        public async Task ChangeNameAsync(string name, long userId)
        {
            var @event = JsonConvert.SerializeObject(
                new CampaignNameChangedEvent {Name = name, ChangedAt = DateTime.UtcNow, ChangedBy = userId},
                _serializerSettings);
            
            RaiseEvent(@event);
            await ConfirmEvents();
        }

        public async Task Start(long userId)
        {
            var @event = JsonConvert.SerializeObject(
                new CampaignStartedEvent {StartedAt = DateTime.UtcNow, UserId = userId},
                _serializerSettings);
            
            RaiseEvent(@event);
            await ConfirmEvents();
        }

        public async Task Pause(long userId)
        {
            var @event = JsonConvert.SerializeObject(
                new CampaignPausedEvent {PausedAt = DateTime.UtcNow, UserId = userId},
                _serializerSettings);
            
            RaiseEvent(@event);
            await ConfirmEvents();
        }
        
        protected override void TransitionState(Campaign state, string @event)
        {
            var deserializedEvent = JsonConvert.DeserializeObject<object>(@event, _serializerSettings);
            switch (@deserializedEvent)
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
                    _logger.LogWarning("Got an {eventType} event, but the state wasn't updated. Current version is {version}.", @event.GetType(), Version);
                    return;
            }
            
            _logger.LogInformation("State updated based on {eventType} event.", @event.GetType());
         }
    }
}