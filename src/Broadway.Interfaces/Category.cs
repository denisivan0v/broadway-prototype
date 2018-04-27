using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces
{
    public sealed class Category
    {
        public long Code { get; set; }
        public bool IsDeleted { get; set; }
        public ISet<Localization> Localizations { get; set; }
        public ISet<long> SecondRubrics { get; set; }
    }
}