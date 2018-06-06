using System.Threading.Tasks;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class CardForERMGrain : JournaledGrain<CardForERM, StateChangedEvent<CardForERM>>, ICardForERMGrain
    {
        private readonly IDataProjector<CardForERM> _dataProjector;

        public CardForERMGrain(IDataProjector<CardForERM> dataProjector)
        {
            _dataProjector = dataProjector;
        }

        public Task<int> GetCurrentVersionAsync() => Task.FromResult(Version);

        public Task ProjectStateAsync() => _dataProjector.ProjectAsync(State);

        [StateModification]
        public async Task UpdateStateAsync(CardForERM cardForErm)
        {
            var oldFirmCode = State.FirmCode;

            RaiseEvent(new StateChangedEvent<CardForERM>(cardForErm));
            await ConfirmEvents();

            var firmGrain = GrainFactory.GetGrain<IFirmGrain>(State.FirmCode);
            if (oldFirmCode != default && oldFirmCode != State.FirmCode)
            {
                await firmGrain.RemoveCardAsync(State.Code);
            }
            else
            {
                await firmGrain.AddCardAsync(State.Code);
            }
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