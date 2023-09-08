using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImminentCrash.Infrastructure
{
    /// <summary>
    /// Only here so we can run dotnet ef commands from this projects.
    /// The given connection string will be the database the commands will use.
    /// </summary>
    internal class DbContextFactory : IDesignTimeDbContextFactory<ImminentCrashDbContext>
    {
        public ImminentCrashDbContext CreateDbContext(string[] args)
        {
            return new ImminentCrashDbContext(new DbContextOptionsBuilder<ImminentCrashDbContext>()
                .UseSqlServer("Server=.;Database=ImminentCrash;User ID=sa;Password=Password0;TrustServerCertificate=True")
                .Options);
        }
    }
}
