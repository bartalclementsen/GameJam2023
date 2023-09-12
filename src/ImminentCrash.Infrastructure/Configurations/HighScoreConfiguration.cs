using ImminentCrash.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImminentCrash.Infrastructure.Configurations;

public class HighScoreConfiguration : IEntityTypeConfiguration<HighScore>
{
    public void Configure(EntityTypeBuilder<HighScore> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.HighscoreTime);
        builder.Property(o => o.Name);
        builder.Property(o => o.DaysAlive);
        builder.Property(o => o.CurrentBalance);
        builder.Property(o => o.HighestBalance);
        builder.Property(o => o.IsDead);
    }
}
