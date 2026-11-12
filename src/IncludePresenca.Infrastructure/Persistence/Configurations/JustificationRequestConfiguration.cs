using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public sealed class JustificationRequestConfiguration : IEntityTypeConfiguration<JustificationRequest>
{
    public void Configure(EntityTypeBuilder<JustificationRequest> builder)
    {
        builder.ToTable("SolicitacoesJustificativas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.SubmittedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DeadlineUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DecidedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.AttachmentStorageKey).HasMaxLength(500);
        builder.Property(x => x.AttachmentOriginalFileName).HasMaxLength(255);
        builder.Property(x => x.AttachmentContentType).HasMaxLength(100);
        builder.HasIndex(x => new { x.ParticipationId, x.Status }).HasFilter("\"Status\" = 'Pending'").IsUnique();
        builder.HasIndex(x => x.MemberId);
        builder.HasIndex(x => x.SubmittedAtUtc);
        builder.HasOne(x => x.Participation).WithMany().HasForeignKey(x => x.ParticipationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DecidedByMember).WithMany().HasForeignKey(x => x.DecidedByMemberId).OnDelete(DeleteBehavior.Restrict);
    }
}
