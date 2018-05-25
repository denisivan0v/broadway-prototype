using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class SecondRubric
    {
        public long Code { get; set; }
        public long CategoryCode { get; set; }
        public bool IsDeleted { get; set; }
        public List<Localization> Localizations { get; set; }
        public ISet<long> Rubrics { get; set; }
    }
}