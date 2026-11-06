using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IncludePresenca.Infrastructure.Persistence.Seed;

public class AdminSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AdminSeeder(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        bool hasMembers = await _context.Members.AnyAsync();

        if (hasMembers)
        {
            return;
        }

        string name = _configuration["AdminSeed:Name"]
            ?? throw new InvalidOperationException("Admin name not configured.");

        string email = _configuration["AdminSeed:Email"]
            ?? throw new InvalidOperationException("Admin email not configured.");

        string password = _configuration["AdminSeed:Password"]
            ?? throw new InvalidOperationException("Admin password not configured.");

        if (password.Length < 8)
        {
            throw new InvalidOperationException("Admin password must contain at least 8 characters.");
        }

        Member admin = new Member(name, email, Role.Diretoria);

        string passwordHash = _passwordHasher.Hash(password);

        admin.ActivateAccount(passwordHash);

        await _context.Members.AddAsync(admin);
        await _context.SaveChangesAsync();
    }
}