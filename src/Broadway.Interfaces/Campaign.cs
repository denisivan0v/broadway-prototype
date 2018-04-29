using System;

namespace NuClear.Broadway.Interfaces
{
    public class Campaign
    {
        public string Name { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public long? StartedBy { get; set; }
        public DateTimeOffset? PausedAt { get; set; }
        public long? PausedBy { get; set; }
    }
}