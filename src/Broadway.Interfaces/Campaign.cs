using System;
using NuClear.Broadway.Interfaces.Events;

namespace NuClear.Broadway.Interfaces
{
    public class Campaign
    {
        public string Name { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public long? StartedBy { get; set; }
        public DateTimeOffset? PausedAt { get; set; }
        public long? PausedBy { get; set; }
        
        public void Apply(CampaignNameChangedEvent nameChangedEvent)
        {
            Name = nameChangedEvent.Name;
        }

        public void Apply(CampaignStartedEvent startedEvent)
        {
            StartedAt = startedEvent.StartedAt;
            StartedBy = startedEvent.UserId;
        }
        
        public void Apply(CampaignPausedEvent pausedEvent)
        {
            PausedAt = pausedEvent.PausedAt;
            PausedBy = pausedEvent.UserId;
        }
    }
}