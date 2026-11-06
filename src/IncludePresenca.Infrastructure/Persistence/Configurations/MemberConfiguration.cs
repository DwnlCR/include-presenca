
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public class MembroConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
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

        builder.Property(member => member.PasswordHash)
            .HasColumnName("SenhaHash")
            .HasMaxLength(500);

        builder.Property(member => member.AccountStatus)
            .HasColumnName("StatusConta")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(member => member.Role)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);
    }
}
