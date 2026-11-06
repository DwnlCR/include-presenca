using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IActivationTokenRepository
{
    Task<ActivationToken?> GetByTokenHashAsync(string tokenHash);
    Task AddAsync(ActivationToken token);
}