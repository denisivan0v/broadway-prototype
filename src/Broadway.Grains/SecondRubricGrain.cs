using System.Threading.Tasks;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class SecondRubricGrain : Grain<SecondRubric>, ISecondRubricGrain
    {
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