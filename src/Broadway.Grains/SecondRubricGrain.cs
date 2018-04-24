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
            State.CategoryCode = secondRubric.CategoryCode;
            State.IsDeleted = secondRubric.IsDeleted;
            State.Localizations = secondRubric.Localizations;
            
            await WriteStateAsync();
        }
    }
}