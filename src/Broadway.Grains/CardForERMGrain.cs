using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class CardForERMGrain : JournaledGrain<CardForERM, StateChangedEvent<CardForERM>>, ICardForERMGrain, IVersionedGrain
    {
        public int GetCurrentVersion() => Version;

        public Task<long> GetFirmCodeAsync() => Task.FromResult(State.FirmCode);

        [StateModification]
        public async Task UpdateStateAsync(CardForERM cardForErm)
        {
            RaiseEvent(new StateChangedEvent<CardForERM>(cardForErm));
            await ConfirmEvents();
        }

        protected override void TransitionState(CardForERM state, StateChangedEvent<CardForERM> @event)
        {
            state.Code = @event.State.Code;
            state.FirmCode = @event.State.FirmCode;
            state.BranchCode = @event.State.BranchCode;
            state.CountryCode = @event.State.CountryCode;
            state.IsLinked = @event.State.IsLinked;
            state.IsActive = @event.State.IsActive;
            state.IsDeleted = @event.State.IsDeleted;
            state.ClosedForAscertainment = @event.State.ClosedForAscertainment;
            state.SortingPosition = @event.State.SortingPosition;
            state.Address = @event.State.Address;
            state.Rubrics = @event.State.Rubrics;
        }
    }
}