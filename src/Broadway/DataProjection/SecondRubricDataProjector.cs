using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.DataProjection
{
    public class SecondRubricDataProjector : IDataProjector<SecondRubric>
    {
        private readonly DataProjectionContext _dbContext;

        public SecondRubricDataProjector(DataProjectionContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ProjectAsync(SecondRubric state)
        {
            var secondRubric = await _dbContext.SecondRubrics.Include(x => x.Localizations).SingleOrDefaultAsync(x => x.Code == state.Code);
            if (secondRubric == null)
            {
                await _dbContext.AddAsync(state);
            }
            else
            {
                _dbContext.Entry(secondRubric).CurrentValues.SetValues(state);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}