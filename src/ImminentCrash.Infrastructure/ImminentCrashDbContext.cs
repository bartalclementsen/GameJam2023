using ImminentCrash.Infrastructure.Configurations;
using ImminentCrash.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;

namespace ImminentCrash.Infrastructure
{
    public class ImminentCrashDbContext : DbContext
    {
        public DbSet<HighScore> HighScores { get; set; }

        public ImminentCrashDbContext(DbContextOptions<ImminentCrashDbContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new HighScoreConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
