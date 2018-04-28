using System.Threading.Tasks;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class CardForERMGrain : Grain<CardForERM>, ICardForERMGrain
    {
        public Task<long> GetFirmCodeAsync() => Task.FromResult(State.FirmCode);

        [StateModification]
        public async Task UpdateStateAsync(CardForERM cardForErm)
        {
            State.Code = cardForErm.Code;
            State.FirmCode = cardForErm.FirmCode;
            State.BranchCode = cardForErm.BranchCode;
            State.CountryCode = cardForErm.CountryCode;
            State.IsLinked = cardForErm.IsLinked;
            State.IsActive = cardForErm.IsActive;
            State.IsDeleted = cardForErm.IsDeleted;
            State.ClosedForAscertainment = cardForErm.ClosedForAscertainment;
            State.SortingPosition = cardForErm.SortingPosition;
            State.Address = cardForErm.Address;
            State.Rubrics = cardForErm.Rubrics;
            
            await WriteStateAsync();
        }
    }
}