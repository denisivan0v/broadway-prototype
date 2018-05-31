using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.DataProjection
{
    public class CategoryDataProjector : IDataProjector<Category>
    {
        private readonly DataProjectionContext _dbContext;

        public CategoryDataProjector(DataProjectionContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ProjectAsync(Category state)
        {
            var category = await _dbContext.Categories.FindAsync(state.Code);
            if (category == null)
            {
                await _dbContext.AddAsync(state);
            }
            else
            {
                _dbContext.Entry(category).CurrentValues.SetValues(state);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}