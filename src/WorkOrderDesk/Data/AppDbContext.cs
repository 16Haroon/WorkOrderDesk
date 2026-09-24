using Microsoft.EntityFrameworkCore;

namespace WorkOrderDesk.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<WorkRequest> WorkRequests => Set<WorkRequest>();
}
