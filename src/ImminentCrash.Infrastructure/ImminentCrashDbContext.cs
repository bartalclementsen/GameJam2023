using Microsoft.EntityFrameworkCore;

namespace ImminentCrash.Infrastructure
{
    public class ImminentCrashDbContext : DbContext
    {
        public ImminentCrashDbContext(DbContextOptions<ImminentCrashDbContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TODO: Add Configuration here
            //modelBuilder.ApplyConfiguration(new SomeClass());

            base.OnModelCreating(modelBuilder);
        }
    }
}
