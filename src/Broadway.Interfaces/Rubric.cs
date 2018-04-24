using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces
{
    public sealed class Rubric
    {
        public int Code { get; set; }
        public int SecondRubricCode { get; set; }
        public bool IsCommercial { get; set; }
        public bool IsDeleted { get; set; }
        public ISet<Localization> Localizations { get; set; }
        public ISet<int> Branches { get; set; }
    }
}