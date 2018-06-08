using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class BranchGrain : JournaledGrain<Branch>, IBranchGrain
    {
        private readonly ILogger<BranchGrain> _logger;
        private readonly IDataProjector<Branch> _dataProjector;

        public BranchGrain(ILogger<BranchGrain> logger, IDataProjector<Branch> dataProjector)
        {
            _logger = logger;
            _dataProjector = dataProjector;
        }

        public Task<int> GetCurrentVersionAsync() => Task.FromResult(Version);

        public async Task ProjectStateAsync() => await _dataProjector.ProjectAsync(State);

        [StateModification]
        public async Task UpdateStateAsync(Branch branch)
        {
            RaiseEvent(new StateChangedEvent<Branch>(branch));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task DeleteAsync(int code)
        {
            RaiseEvent(new ObjectDeletedEvent(code));
            await ConfirmEvents();
        }

        protected override void TransitionState(Branch state, object @event)
        {
            switch (@event)
            {
                case StateChangedEvent<Branch> stateChangedEvent:
                    state.Code = stateChangedEvent.State.Code;
                    state.DefaultCityCode = stateChangedEvent.State.DefaultCityCode;
                    state.DefaultCountryCode = stateChangedEvent.State.DefaultCountryCode;
                    state.DefaultLang = stateChangedEvent.State.DefaultLang;
                    state.IsOnInfoRussia = stateChangedEvent.State.IsOnInfoRussia;
                    state.NameLat = stateChangedEvent.State.NameLat;
                    state.IsDeleted = stateChangedEvent.State.IsDeleted;
                    state.EnabledLanguages = stateChangedEvent.State.EnabledLanguages;
                    state.Localizations = stateChangedEvent.State.Localizations;

                    break;
                case ObjectDeletedEvent objectDeletedEvent:
                    state.Code = (int)objectDeletedEvent.Id;
                    state.IsDeleted = true;

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
