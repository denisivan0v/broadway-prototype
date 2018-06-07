using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.DataProjection
{
    public class FirmDataProjector : IDataProjector<Firm>
    {
        private readonly DataProjectionContext _dbContext;

        public FirmDataProjector(DataProjectionContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ProjectAsync(Firm state)
        {
            var firm = await _dbContext.Firms.SingleOrDefaultAsync(x => x.Code == state.Code);
            if (firm == null)
            {
                await _dbContext.AddAsync(state);
            }
            else
            {
                _dbContext.Entry(firm).CurrentValues.SetValues(state);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}