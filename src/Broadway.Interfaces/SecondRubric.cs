using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces
{
    public sealed class SecondRubric
    {
        public long Code { get; set; }
        public long CategoryCode { get; set; }
        public bool IsDeleted { get; set; }
        public ISet<Localization> Localizations { get; set; }
    }
}