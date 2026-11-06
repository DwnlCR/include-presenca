using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncludePresenca.Infrastructure.Persistence.Configurations;

public class ActivationTokenConfiguration : IEntityTypeConfiguration<ActivationToken>
{
    public void Configure(EntityTypeBuilder<ActivationToken> builder)
    {
        builder.ToTable("ActivationTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .ValueGeneratedNever();

        builder.Property(token => token.MemberId)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.Property(token => token.UsedAt);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(token => token.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}