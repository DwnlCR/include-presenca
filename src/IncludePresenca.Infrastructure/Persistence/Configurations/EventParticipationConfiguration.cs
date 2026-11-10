
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
{
    public void Configure(EntityTypeBuilder<EventParticipation> builder)
    {
        builder.ToTable("ParticipacoesEventos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.EventId)
            .IsRequired();

        builder.Property(p => p.MemberId)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30)
            .HasSentinel(EventParticipationStatus.Pending)
            .HasDefaultValue(EventParticipationStatus.Pending);

        builder.Property(p => p.CheckedInAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.Justification)
            .HasMaxLength(2000);

        builder.Property(p => p.JustifiedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.EventId, p.MemberId })
            .IsUnique();

        builder.HasOne(p => p.Event)
            .WithMany()
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Member)
            .WithMany()
            .HasForeignKey(p => p.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
