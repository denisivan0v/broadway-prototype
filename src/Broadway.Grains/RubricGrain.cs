using System.Threading.Tasks;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class RubricGrain : Grain<Rubric>, IRubricGrain
    {
        public async Task UpdateStateAsync(Rubric rubric)
        {
            State.Code = rubric.Code;
            State.IsDeleted = rubric.IsDeleted;

            if (!rubric.IsDeleted)
            {
                State.SecondRubricCode = rubric.SecondRubricCode;
                State.IsCommercial = rubric.IsCommercial;
                State.Localizations = rubric.Localizations;
                State.Branches = rubric.Branches;
            }

            await WriteStateAsync();
        }
    }
}