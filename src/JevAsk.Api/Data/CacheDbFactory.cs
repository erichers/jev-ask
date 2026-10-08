using JevAsk.Api.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JevAsk.Api.Data;

public sealed class CacheDbFactory : IDesignTimeDbContextFactory<CacheDb>
{
    public CacheDb CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CacheDb>();
        options.UseSqlite("Data Source=design-time.db");
        return new CacheDb(options.Options);
    }
}
