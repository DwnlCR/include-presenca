using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IEventParticipationRepository
{
    Task AddRangeAsync(List<EventParticipation> participations);
}