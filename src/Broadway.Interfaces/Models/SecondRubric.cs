using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class SecondRubric
    {
        public long Code { get; set; }
        public long CategoryCode { get; set; }
        public bool IsDeleted { get; set; }
        public ICollection<RubricLocalization> Localizations { get; set; }
        public ISet<long> Rubrics { get; private set; }

        public void AddRubric(long rubricCode)
        {
            if (Rubrics != default)
            {
                if (Rubrics.Contains(rubricCode))
                {
                    return;
                }
            }
            else
            {
                Rubrics = new HashSet<long>();
            }

            Rubrics.Add(rubricCode);
        }

        public void RemoveRubric(long rubricCode)
        {
            Rubrics?.Remove(rubricCode);
        }
    }
}