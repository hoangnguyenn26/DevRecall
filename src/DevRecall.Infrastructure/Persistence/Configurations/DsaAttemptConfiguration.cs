using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class DsaAttemptConfiguration
    : IEntityTypeConfiguration<DsaAttempt>
{
    public void Configure(EntityTypeBuilder<DsaAttempt> builder)
    {
        builder.ToTable("dsa_attempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Id).ValueGeneratedNever();
        builder.Property(attempt => attempt.DsaProblemId).IsRequired();
        builder.Property(attempt => attempt.AttemptNumber).IsRequired();
        builder.Property(attempt => attempt.Result)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(attempt => attempt.Language)
            .HasMaxLength(DsaAttemptText.LanguageMaxLength);
        builder.Property(attempt => attempt.SolutionCode).HasColumnType("text");
        builder.Property(attempt => attempt.Approach).HasColumnType("text");
        builder.Property(attempt => attempt.TimeComplexity)
            .HasMaxLength(DsaAttemptText.ComplexityMaxLength);
        builder.Property(attempt => attempt.SpaceComplexity)
            .HasMaxLength(DsaAttemptText.ComplexityMaxLength);
        builder.Property(attempt => attempt.DurationMinutes).IsRequired();
        builder.Property(attempt => attempt.Notes).HasColumnType("text");
        builder.Property(attempt => attempt.AttemptedAtUtc).IsRequired();
        builder.Property(attempt => attempt.CreatedAtUtc).IsRequired();

        builder.HasOne<DsaProblem>()
            .WithMany()
            .HasForeignKey(attempt => attempt.DsaProblemId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureIndexes(builder);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<DsaAttempt> builder)
    {
        builder.HasIndex(attempt => new
        {
            attempt.DsaProblemId,
            attempt.AttemptNumber
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_dsa_attempts_problem_attempt_number");

        builder.HasIndex(attempt => new
        {
            attempt.DsaProblemId,
            attempt.AttemptedAtUtc
        })
            .HasDatabaseName(
                "ix_dsa_attempts_problem_attempted_at");

        builder.HasIndex(attempt => new
        {
            attempt.DsaProblemId,
            attempt.Result,
            attempt.AttemptNumber
        })
            .HasDatabaseName(
                "ix_dsa_attempts_problem_result_number");
    }
}
