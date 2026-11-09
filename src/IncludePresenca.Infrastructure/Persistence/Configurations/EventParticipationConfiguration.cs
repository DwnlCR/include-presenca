
using IncludePresenca.Domain.Entities;
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
