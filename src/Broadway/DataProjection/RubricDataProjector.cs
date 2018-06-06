using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.DataProjection
{
    public class RubricDataProjector : IDataProjector<Rubric>
    {
        private readonly DataProjectionContext _dbContext;

        public RubricDataProjector(DataProjectionContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ProjectAsync(Rubric state)
        {
            var rubric = await _dbContext.Rubrics.Include(x => x.Localizations).SingleOrDefaultAsync(x => x.Code == state.Code);
            if (rubric == null)
            {
                await _dbContext.AddAsync(state);
            }
            else
            {
                _dbContext.Entry(rubric).CurrentValues.SetValues(state);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}