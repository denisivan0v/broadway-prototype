using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans;
using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class FirmGrain : JournaledGrain<Firm>, IFirmGrain
    {
        private readonly ILogger<FirmGrain> _logger;
        private readonly IDataProjector<Firm> _dataProjector;

        public FirmGrain(ILogger<FirmGrain> logger, IDataProjector<Firm> dataProjector)
        {
            _logger = logger;
            _dataProjector = dataProjector;
        }

        public Task<int> GetCurrentVersionAsync() => Task.FromResult(Version);

        public Task ProjectStateAsync() => _dataProjector.ProjectAsync(State);

        [StateModification]
        public async Task AddCardAsync(long cardCode)
        {
            RaiseEvent(new CardAddedToFirmEvent(cardCode));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task RemoveCardAsync(long cardCode)
        {
            RaiseEvent(new CardRemovedFromFirmEvent(cardCode));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task Archive(int branchCode, int? countryCode)
        {
            var firmCode = State.Code;
            if (firmCode == default)
            {
                firmCode = this.GetPrimaryKeyLong();
            }

            RaiseEvent(new FirmArchivedEvent(firmCode, branchCode, countryCode));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task UpdateStateAsync(Firm firm)
        {
            RaiseEvent(new StateChangedEvent<Firm>(firm));
            await ConfirmEvents();
        }

        protected override void TransitionState(Firm state, object @event)
        {
            switch (@event)
            {
                case CardAddedToFirmEvent cardAddedToFirmEvent:
                    state.AddCard(cardAddedToFirmEvent.CardCode);
                    break;
                case CardRemovedFromFirmEvent cardRemovedFromFirmEvent:
                    state.RemoveCard(cardRemovedFromFirmEvent.CardCode);
                    break;
                case FirmArchivedEvent firmArchievedEvent:
                    state.Code = firmArchievedEvent.FirmCode;
                    state.BranchCode = firmArchievedEvent.BranchCode;
                    state.IsActive = false;
                    state.IsArchived = true;
                    state.Cards = null;
                    break;
                case StateChangedEvent<Firm> stateChangedEvent:
                    state.Code = stateChangedEvent.State.Code;
                    state.BranchCode = stateChangedEvent.State.BranchCode;
                    state.Name = stateChangedEvent.State.Name;
                    state.IsActive = stateChangedEvent.State.IsActive;
                    state.ClosedForAscertainment = stateChangedEvent.State.ClosedForAscertainment;
                    break;
                default:
                    _logger.LogWarning(
                        "Got an {eventType} event, but the state wasn't updated. Current version is {version}.",
                        @event.GetType(),
                        Version);
                    return;
            }
        }
    }
}