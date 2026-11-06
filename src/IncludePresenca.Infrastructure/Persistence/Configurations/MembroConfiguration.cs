using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public class MembroConfiguration : IEntityTypeConfiguration<Membro>
{
    public void Configure(EntityTypeBuilder<Membro> builder)
    {
        builder.ToTable("Membros");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(member => member.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(member => member.Email)
            .IsUnique();

        builder.Property(member => member.SenhaHash)
            .HasMaxLength(500);

        builder.Property(member => member.StatusConta)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(member => member.Role)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);
    }
}