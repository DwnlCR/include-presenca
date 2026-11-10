using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public class EventCheckInCodeConfiguration : IEntityTypeConfiguration<EventCheckInCode>
{
    public void Configure(EntityTypeBuilder<EventCheckInCode> builder)
    {
        builder.ToTable("CodigosCheckin");

        builder.HasKey(code => code.Id);

        builder.Property(code => code.EventId)
            .IsRequired();

        builder.Property(code => code.CodeHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(code => code.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(code => code.ExpiresAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(code => code.EventId)
            .IsUnique();

        builder.HasOne(code => code.Event)
            .WithMany()
            .HasForeignKey(code => code.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
