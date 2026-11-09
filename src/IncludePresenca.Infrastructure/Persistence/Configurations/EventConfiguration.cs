
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Eventos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.StartsAt)
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .IsRequired();

        builder.Property(e => e.IsCancelled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(e => e.StartsAt);
    }
}
