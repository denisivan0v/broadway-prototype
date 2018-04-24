using System.Threading.Tasks;
using NuClear.Broadway.Interfaces;
using Orleans;

namespace NuClear.Broadway.Grains
{
    public class CategoryGrain : Grain<Category>, ICategoryGrain
    {
        public override Task OnActivateAsync()
        {
            var a = "asd";
            return base.OnActivateAsync();
        }

        public async Task UpdateStateAsync(Category category)
        {
            State.Code = category.Code;
            State.IsDeleted = category.IsDeleted;
            State.Localizations = category.Localizations;
            
            await WriteStateAsync();
        }
    }
}