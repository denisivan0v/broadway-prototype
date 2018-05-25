using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class Category
    {
        public long Code { get; set; }
        public bool IsDeleted { get; set; }
        public List<Localization> Localizations { get; set; }
        public ISet<long> SecondRubrics { get; set; }
    }
}