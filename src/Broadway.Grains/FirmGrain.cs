using System.Collections.Generic;
using System.Threading.Tasks;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class FirmGrain : Grain<Firm>, IFirmGrain
    {
        [StateModification]
        public async Task AddCardAsync(long cardCode)
        {
            if (State.Cards != null)
            {
                if (State.Cards.Contains(cardCode))
                {
                    return;
                }
            }
            else
            {
                State.Cards = new HashSet<long>();
            }

            State.Cards.Add(cardCode);
            await WriteStateAsync();
        }

        [StateModification]
        public async Task RemoveCardAsync(long cardCode)
        {
            if (State.Cards != null)
            {
                State.Cards.Remove(cardCode);
                await WriteStateAsync();
            }
        }

        [StateModification]
        public Task Archive()
        {
            return Task.CompletedTask;
        }

        [StateModification]
        public async Task UpdateStateAsync(Firm firm)
        {
            State.Code = firm.Code;
            State.BranchCode = firm.BranchCode;
            State.Name = firm.Name;
            State.IsActive = firm.IsActive;
            State.ClosedForAscertainment = firm.ClosedForAscertainment;

            await WriteStateAsync();
        }
    }
}