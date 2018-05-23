using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class CardForERM
    {
        public long Code { get; set; }
        public long FirmCode { get; set; }
        public int BranchCode { get; set; }
        public int? CountryCode { get; set; }
        public bool IsLinked { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool ClosedForAscertainment { get; set; }
        public int SortingPosition { get; set; }
        public FirmAddress Address { get; set; }
        public ISet<Rubric> Rubrics { get; set; }

        public sealed class FirmAddress
        {
            public string Text { get; set; }
            public long? TerritoryCode { get; set; }
            public int? BuildingPurposeCode { get; set; }
        }

        public sealed class Rubric
        {
            public long Code { get; set; }
            public bool IsPrimary { get; set; }
            public int SortingPosition { get; set; }
        }
    }
}