using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hominder.Infrastructure.Persistence;

public sealed class HominderDbContextFactory : IDesignTimeDbContextFactory<HominderDbContext>
{
    public HominderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HominderDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=hominder;Username=hominder;Password=hominder")
            .Options;

        return new HominderDbContext(options);
    }
}
