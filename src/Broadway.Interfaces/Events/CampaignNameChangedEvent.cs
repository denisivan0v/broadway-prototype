using System;

namespace NuClear.Broadway.Interfaces.Events
{
    public class CampaignNameChangedEvent
    {
        public string Name { get; set; }
        public DateTimeOffset ChangedAt { get; set; }
        public long ChangedBy { get; set; }
    }
}