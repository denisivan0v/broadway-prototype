using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class Branch
    {
        public int Code { get; set; }
        public string NameLat { get; set; }
        public int DefaultCountryCode { get; set; }
        public long? DefaultCityCode { get; set; }
        public string DefaultLang { get; set; }
        public bool IsOnInfoRussia { get; set; }
        public bool IsDeleted { get; set; }
        public ICollection<BranchLocalization> Localizations { get; set; }
        public ICollection<string> EnabledLanguages { get; set; }
    }
}