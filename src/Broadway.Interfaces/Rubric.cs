using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces
{
    public sealed class Rubric
    {
        public long Code { get; set; }
        public long SecondRubricCode { get; set; }
        public bool IsCommercial { get; set; }
        public bool IsDeleted { get; set; }
        public ISet<Localization> Localizations { get; set; }
        public ISet<int> Branches { get; set; }
    }
}