using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Configurations;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> b)
    {
        b.ToTable("Surveys", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(Survey.NameMaxLength).IsRequired();
        b.Property(x => x.SiteName).HasMaxLength(Survey.SiteNameMaxLength).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(Survey.NotesMaxLength);
        b.Property(x => x.SurveyDate).HasColumnType("date");
        b.Property(x => x.StartTime).HasColumnType("time(0)");

        // Enums stored as readable strings (easier to query and report on).
        b.Property(x => x.Season).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Camera).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.TimeWindow).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.GroundCover).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => x.CreatedAt);
        b.HasMany(x => x.Images).WithOne(i => i.Survey).HasForeignKey(i => i.SurveyId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x => x.TrainingSplit).HasConversion<string>().HasMaxLength(12);
    }
}
