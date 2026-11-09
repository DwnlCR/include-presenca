using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<List<Event>> GetAllAsync();
    Task AddAsync(Event eventEntity);
}