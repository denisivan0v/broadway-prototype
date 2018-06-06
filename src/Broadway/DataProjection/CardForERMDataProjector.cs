using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.DataProjection
{
    public class CardForERMDataProjector : IDataProjector<CardForERM>
    {
        private readonly DataProjectionContext _dbContext;

        public CardForERMDataProjector(DataProjectionContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ProjectAsync(CardForERM state)
        {
            var card = await _dbContext.Cards.Include(x => x.Rubrics).SingleOrDefaultAsync(x => x.Code == state.Code);
            if (card == null)
            {
                await _dbContext.AddAsync(state);
            }
            else
            {
                _dbContext.Entry(card).CurrentValues.SetValues(state);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}