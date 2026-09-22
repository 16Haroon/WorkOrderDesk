using Microsoft.EntityFrameworkCore;

namespace WorkOrderDesk.Data;

// The connection to workorders.db. WorkRequests is the table.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<WorkRequest> WorkRequests => Set<WorkRequest>();
}
