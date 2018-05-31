using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Events;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

using Orleans.EventSourcing;

namespace NuClear.Broadway.Grains
{
    public class CategoryGrain : JournaledGrain<Category>, ICategoryGrain
    {
        private readonly ILogger<CategoryGrain> _logger;
        private readonly IDataProjector<Category> _dataProjector;

        public CategoryGrain(ILogger<CategoryGrain> logger, IDataProjector<Category> dataProjector)
        {
            _logger = logger;
            _dataProjector = dataProjector;
        }

        public Task<int> GetCurrentVersionAsync() => Task.FromResult(Version);

        public async Task ProjectStateAsync() => await _dataProjector.ProjectAsync(State);

        [StateModification]
        public async Task AddSecondRubricAsync(long secondRubricCode)
        {
            if (State.SecondRubrics != null)
            {
                if (State.SecondRubrics.Contains(secondRubricCode))
                {
                    return;
                }
            }
            else
            {
                State.SecondRubrics = new HashSet<long>();
            }

            RaiseEvent(new SecondRubricAddedToCategoryEvent(secondRubricCode));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task RemoveSecondRubricAsync(long secondRubricCode)
        {
            if (State.SecondRubrics != null)
            {
                RaiseEvent(new SecondRubricRemovedFromCategoryEvent(secondRubricCode));
                await ConfirmEvents();
            }
        }

        [StateModification]
        public async Task UpdateStateAsync(Category category)
        {
            RaiseEvent(new StateChangedEvent<Category>(category));
            await ConfirmEvents();
        }

        [StateModification]
        public async Task DeleteAsync(long code)
        {
            RaiseEvent(new ObjectDeletedEvent(code));
            await ConfirmEvents();
        }

        protected override void TransitionState(Category state, object @event)
        {
            switch (@event)
            {
                case SecondRubricAddedToCategoryEvent secondRubricAddedToCategoryEvent:
                    state.SecondRubrics.Add(secondRubricAddedToCategoryEvent.SecondRubricCode);
                    break;
                case SecondRubricRemovedFromCategoryEvent secondRubricRemovedFromCategoryEvent:
                    state.SecondRubrics.Remove(secondRubricRemovedFromCategoryEvent.SecondRubricCode);
                    break;
                case StateChangedEvent<Category> stateChangedEvent:
                    state.Code = stateChangedEvent.State.Code;
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