using System.Collections.Generic;
using System.Threading.Tasks;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class SecondRubricGrain : Grain<SecondRubric>, ISecondRubricGrain
    {
        [StateModification]
        public async Task AddRubric(long rubricCode)
        {
            if (State.Rubrics != null)
            {
                if (State.Rubrics.Contains(rubricCode))
                {
                    return;
                }
            }
            else
            {
                State.Rubrics = new HashSet<long>();
            }

            State.Rubrics.Add(rubricCode);
            await WriteStateAsync();
        }

        [StateModification]
        public async Task RemoveRubric(long rubricCode)
        {
            if (State.Rubrics != null)
            {
                State.Rubrics.Remove(rubricCode);
                await WriteStateAsync();
            }
        }

        [StateModification]
        public async Task UpdateStateAsync(SecondRubric secondRubric)
        {
            State.Code = secondRubric.Code;
            State.IsDeleted = secondRubric.IsDeleted;

            if (!secondRubric.IsDeleted)
            {
                State.CategoryCode = secondRubric.CategoryCode;
                State.Localizations = secondRubric.Localizations;
            }

            await WriteStateAsync();
        }
    }
}