using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EduZim.Infrastructure.Persistence;

public sealed class EduZimDbContextFactory : IDesignTimeDbContextFactory<EduZimDbContext>
{
    public EduZimDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EduZimDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=eduzim;Username=eduzim;Password=eduzim_dev");

        var dataProtection = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(Path.GetTempPath(), "eduzim-ef-design")));

        return new EduZimDbContext(optionsBuilder.Options, dataProtection);
    }
}
