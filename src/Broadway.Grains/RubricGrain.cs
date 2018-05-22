using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Interfaces.Events;

using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class RubricGrain : JournaledGrain<Rubric>, IRubricGrain, IVersionedGrain
    {
        private readonly ILogger<Rubric> _logger;

        public RubricGrain(ILogger<Rubric> logger)
        {
            _logger = logger;
        }

        public int GetCurrentVersion() => Version;

        [StateModification]
        public async Task UpdateStateAsync(Rubric rubric)
        {
            RaiseEvent(new StateChangedEvent<Rubric>(rubric));
            await ConfirmEvents();

            var secondRubricGrain = GrainFactory.GetGrain<ISecondRubricGrain>(State.SecondRubricCode);
            await secondRubricGrain.AddRubricAsync(State.Code);
        }

        [StateModification]
        public async Task DeleteAsync()
        {
            var secondRubricGrain = GrainFactory.GetGrain<ISecondRubricGrain>(State.SecondRubricCode);
            await secondRubricGrain.RemoveRubricAsync(State.Code);

            RaiseEvent(new ObjectDeletedEvent());
            await ConfirmEvents();
        }

        protected override void TransitionState(Rubric state, object @event)
        {
            switch (@event)
            {
                case StateChangedEvent<Rubric> stateChangedEvent:
                    state.Code = stateChangedEvent.State.Code;
                    state.SecondRubricCode = stateChangedEvent.State.SecondRubricCode;
                    state.IsCommercial = stateChangedEvent.State.IsCommercial;
                    state.Localizations = stateChangedEvent.State.Localizations;
                    state.Branches = stateChangedEvent.State.Branches;

                    break;
                case ObjectDeletedEvent _:
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