using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class RubricGrain : JournaledGrain<Rubric>, IRubricGrain
    {
        private readonly ILogger<Rubric> _logger;
        private readonly IDataProjector<Rubric> _dataProjector;

        public RubricGrain(ILogger<Rubric> logger, IDataProjector<Rubric> dataProjector)
        {
            _logger = logger;
            _dataProjector = dataProjector;
        }

        public Task<int> GetCurrentVersionAsync() => Task.FromResult(Version);

        public async Task ProjectStateAsync() => await _dataProjector.ProjectAsync(State);

        [StateModification]
        public async Task UpdateStateAsync(Rubric rubric)
        {
            RaiseEvent(new StateChangedEvent<Rubric>(rubric));
            await ConfirmEvents();

            var secondRubricGrain = GrainFactory.GetGrain<ISecondRubricGrain>(State.SecondRubricCode);
            await secondRubricGrain.AddRubricAsync(State.Code);
        }

        [StateModification]
        public async Task DeleteAsync(long code)
        {
            if (State.SecondRubricCode != default)
            {
                var secondRubricGrain = GrainFactory.GetGrain<ISecondRubricGrain>(State.SecondRubricCode);
                await secondRubricGrain.RemoveRubricAsync(State.Code);
            }

            RaiseEvent(new ObjectDeletedEvent(code));
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
                case ObjectDeletedEvent objectDeletedEvent:
                    state.Code = objectDeletedEvent.Id;
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