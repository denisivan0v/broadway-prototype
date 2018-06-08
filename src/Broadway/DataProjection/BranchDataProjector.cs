using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.DataProjection
{
    public class BranchDataProjector : IDataProjector<Branch>
    {
        private readonly DataProjectionContext _dbContext;

        public BranchDataProjector(DataProjectionContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ProjectAsync(Branch state)
        {
            var branch = await _dbContext.Branches
                                         .Include(x => x.Localizations)
                                         .SingleOrDefaultAsync(x => x.Code == state.Code);

            if (branch == null)
            {
                await _dbContext.AddAsync(state);
            }
            else
            {
                _dbContext.Entry(branch).CurrentValues.SetValues(state);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}