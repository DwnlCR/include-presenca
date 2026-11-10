
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<ActivationToken> ActivationTokens => Set<ActivationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EventParticipation> EventParticipations => Set<EventParticipation>();
    public DbSet<EventCheckInCode> EventCheckInCodes => Set<EventCheckInCode>();

    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
