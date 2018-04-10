using System;

namespace NuClear.Broadway.Interfaces.Events
{
    public class CampaignStartedEvent
    {
        public DateTimeOffset StartedAt { get; set; }
        public long UserId { get; set; }
    }
}