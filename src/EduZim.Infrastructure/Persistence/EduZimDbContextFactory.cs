using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EduZim.Infrastructure.Persistence;

public sealed class EduZimDbContextFactory : IDesignTimeDbContextFactory<EduZimDbContext>
{
    public EduZimDbContext CreateDbContext(string[] args)
    {
        // Align with appsettings.json DefaultConnection; override via ConnectionStrings__DefaultConnection.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=EduZImDB;Username=postgres;Password=root123";

        var optionsBuilder = new DbContextOptionsBuilder<EduZimDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        var dataProtection = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(Path.GetTempPath(), "eduzim-ef-design")));

        return new EduZimDbContext(optionsBuilder.Options, dataProtection);
    }
}
