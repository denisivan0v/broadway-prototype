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
        
        public override Task OnActivateAsync()
        {
            _logger.LogInformation("Activating...");
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