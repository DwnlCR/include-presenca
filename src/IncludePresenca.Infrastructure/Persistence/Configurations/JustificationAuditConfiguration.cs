using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public sealed class JustificationAuditConfiguration : IEntityTypeConfiguration<JustificationAudit>
{
    public void Configure(EntityTypeBuilder<JustificationAudit> builder)
    {
        builder.ToTable("AuditoriaSolicitacoesJustificativas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.PreviousParticipationStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.NewParticipationStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Origin).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ChangedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.JustificationRequestId, x.ChangedAtUtc });
        builder.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.JustificationRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Participation).WithMany().HasForeignKey(x => x.ParticipationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ChangedByMember).WithMany().HasForeignKey(x => x.ChangedByMemberId).OnDelete(DeleteBehavior.Restrict);
    }
}
