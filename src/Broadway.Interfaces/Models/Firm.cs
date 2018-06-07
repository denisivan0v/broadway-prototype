using System.Collections.Generic;

namespace NuClear.Broadway.Interfaces.Models
{
    public sealed class Firm
    {
        public long Code { get; set; }
        public int BranchCode { get; set; }
        public int? CountryCode { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool ClosedForAscertainment { get; set; }
        public bool IsArchived { get; set; }
        public ISet<long> Cards { get; set; }

        public void AddCard(long cardCode)
        {
            if (Cards != null)
            {
                if (Cards.Contains(cardCode))
                {
                    return;
                }
            }
            else
            {
                Cards = new HashSet<long>();
            }

            Cards.Add(cardCode);
        }

        public void RemoveCard(long cardCode)
        {
            Cards?.Remove(cardCode);
        }
    }
}