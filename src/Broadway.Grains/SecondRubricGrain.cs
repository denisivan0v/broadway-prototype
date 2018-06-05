using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class SecondRubricGrain : JournaledGrain<SecondRubric>, ISecondRubricGrain
    {
        private readonly ILogger<SecondRubricGrain> _logger;
        private readonly IDataProjector<SecondRubric> _dataProjector;

        public SecondRubricGrain(ILogger<SecondRubricGrain> logger, IDataProjector<SecondRubric> dataProjector)
        {
            _logger = logger;
            _dataProjector = dataProjector;
        }

        public Task<int> GetCurrentVersionAsync() => Task.FromResult(Version);

        public async Task ProjectStateAsync() => await _dataProjector.ProjectAsync(State);

        [StateModification]
        public async Task AddRubricAsync(long rubricCode)
        {
            RaiseEvent(new RubricAddedToSecondRubricEvent(rubricCode));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task RemoveRubricAsync(long rubricCode)
        {
            RaiseEvent(new RubricRemovedFromSecondRubricEvent(rubricCode));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task UpdateStateAsync(SecondRubric secondRubric)
        {
            RaiseEvent(new StateChangedEvent<SecondRubric>(secondRubric));
            await ConfirmEvents();

            var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(State.CategoryCode);
            await categoryGrain.AddSecondRubricAsync(State.Code);
        }

        [StateModification]
        public async Task DeleteAsync(long code)
        {
            if (State.CategoryCode != default)
            {
                var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(State.CategoryCode);
                await categoryGrain.RemoveSecondRubricAsync(State.Code);
            }

            RaiseEvent(new ObjectDeletedEvent(code));
            await ConfirmEvents();
        }

        protected override void TransitionState(SecondRubric state, object @event)
        {
            switch (@event)
            {
                case RubricAddedToSecondRubricEvent rubricAddedToSecondRubricEvent:
                    state.AddRubric(rubricAddedToSecondRubricEvent.RubricCode);

                    break;
                case RubricRemovedFromSecondRubricEvent rubricRemovedFromSecondRubricEvent:
                    state.RemoveRubric(rubricRemovedFromSecondRubricEvent.RubricCode);

                    break;
                case StateChangedEvent<SecondRubric> stateChangedEvent:
                    state.Code = stateChangedEvent.State.Code;
                    state.CategoryCode = stateChangedEvent.State.CategoryCode;
                    state.Localizations = stateChangedEvent.State.Localizations;

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