using Microsoft.EntityFrameworkCore;

namespace StockReplenishmentAPI.Models;

public class StockDbContext : DbContext
{
    public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
    {
    }

    public DbSet<StockRequest> StockRequests  => Set<StockRequest>();
}