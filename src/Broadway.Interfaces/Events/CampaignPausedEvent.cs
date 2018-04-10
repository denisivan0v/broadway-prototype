using System;

namespace NuClear.Broadway.Interfaces.Events
{
    public class CampaignPausedEvent
    {
        public DateTimeOffset PausedAt { get; set; }
        public long UserId { get; set; }
    }
}