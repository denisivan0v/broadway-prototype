using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class CategoryGrain : Grain<Category>, ICategoryGrain
    {
        private readonly ILogger<CategoryGrain> _logger;

        public CategoryGrain(ILogger<CategoryGrain> logger)
        {
            _logger = logger;
        }

        [StateModification]
        public async Task AddSecondRubric(long secondRubricCode)
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

            State.SecondRubrics.Add(secondRubricCode);
            await WriteStateAsync();
        }

        [StateModification]
        public async Task RemoveSecondRubric(long secondRubricCode)
        {
            if (State.SecondRubrics != null)
            {
                State.SecondRubrics.Remove(secondRubricCode);
                await WriteStateAsync();
            }
        }

        [StateModification]
        public async Task UpdateStateAsync(Category category)
        {
            State.Code = category.Code;
            State.IsDeleted = category.IsDeleted;

            if (!category.IsDeleted)
            {
                State.Localizations = category.Localizations;
            }

            await WriteStateAsync();
        }
    }
}