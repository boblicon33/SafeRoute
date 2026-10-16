using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SafeRoute.MonitorService.Data;

public class MonitorDbContextFactory : IDesignTimeDbContextFactory<MonitorDbContext>
{
    public MonitorDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<MonitorDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b => 
    b.MigrationsHistoryTable("__EFMigrationsHistory", "monitor"));

        return new MonitorDbContext(optionsBuilder.Options);
    }
}