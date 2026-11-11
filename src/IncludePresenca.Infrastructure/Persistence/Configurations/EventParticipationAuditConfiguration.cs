using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public class EventParticipationAuditConfiguration : IEntityTypeConfiguration<EventParticipationAudit>
{
    public void Configure(EntityTypeBuilder<EventParticipationAudit> builder)
    {
        builder.ToTable("AuditoriaParticipacoesEventos");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.ParticipationId)
            .IsRequired();

        builder.Property(audit => audit.EventId)
            .IsRequired();

        builder.Property(audit => audit.MemberId)
            .IsRequired();

        builder.Property(audit => audit.ChangedByMemberId)
            .IsRequired();

        builder.Property(audit => audit.ChangedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(audit => audit.PreviousStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(audit => audit.NewStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(audit => audit.PreviousCheckedInAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(audit => audit.NewCheckedInAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(audit => audit.PreviousJustification)
            .HasMaxLength(2000);

        builder.Property(audit => audit.NewJustification)
            .HasMaxLength(2000);

        builder.Property(audit => audit.PreviousJustifiedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(audit => audit.NewJustifiedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(audit => audit.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(audit => new { audit.ParticipationId, audit.ChangedAtUtc });
        builder.HasIndex(audit => audit.ChangedAtUtc);

        builder.HasOne(audit => audit.Participation)
            .WithMany()
            .HasForeignKey(audit => audit.ParticipationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(audit => audit.Event)
            .WithMany()
            .HasForeignKey(audit => audit.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(audit => audit.Member)
            .WithMany()
            .HasForeignKey(audit => audit.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(audit => audit.ChangedByMember)
            .WithMany()
            .HasForeignKey(audit => audit.ChangedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
