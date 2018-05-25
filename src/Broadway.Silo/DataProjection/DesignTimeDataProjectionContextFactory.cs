using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using NuClear.Broadway.DataProjection;

namespace NuClear.Broadway.Silo.DataProjection
{
    public sealed class DesignTimeDataProjectionContextFactory : IDesignTimeDbContextFactory<DataProjectionContext>
    {
        public DataProjectionContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataProjectionContext>()
                     .UseNpgsql("Host=localhost;Username=postgres;Password=postgres;Database=BroadwayDataProjection");

            return new DataProjectionContext(optionsBuilder.Options);
        }
    }
}