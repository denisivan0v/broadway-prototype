using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class Category
    {
        public long Code { get; set; }
        public bool IsDeleted { get; set; }
        public List<RubricLocalization> Localizations { get; set; }
        public ISet<long> SecondRubrics { get; private set; }

        public void AddSecondRubric(long secondRubricCode)
        {
            if (SecondRubrics != default)
            {
                if (SecondRubrics.Contains(secondRubricCode))
                {
                    return;
                }
            }
            else
            {
                SecondRubrics = new HashSet<long>();
            }

            SecondRubrics.Add(secondRubricCode);
        }

        public void RemoveSecondRubric(long secondRubricCode)
        {
            SecondRubrics?.Remove(secondRubricCode);
        }
    }
}