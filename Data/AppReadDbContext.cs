using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Data;

public class AppReadDbContext : AppDbContext
{
    public AppReadDbContext(DbContextOptions<AppReadDbContext> options) : base(options) { }
}
