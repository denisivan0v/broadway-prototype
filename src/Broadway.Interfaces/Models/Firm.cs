using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class Firm
    {
        public long Code { get; set; }
        public int BranchCode { get; set; }
        public int? CountryCode { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool ClosedForAscertainment { get; set; }
        public bool IsArchived { get; set; }
        public ISet<long> Cards { get; set; }
    }
}