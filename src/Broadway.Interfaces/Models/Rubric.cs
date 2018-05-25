using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class Rubric
    {
        public long Code { get; set; }
        public long SecondRubricCode { get; set; }
        public bool IsCommercial { get; set; }
        public bool IsDeleted { get; set; }
        public List<Localization> Localizations { get; set; }
        public ISet<int> Branches { get; set; }
    }
}