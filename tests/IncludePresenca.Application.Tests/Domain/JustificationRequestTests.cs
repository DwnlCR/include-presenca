using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Xunit;

namespace IncludePresenca.Application.Tests.Domain;

public class JustificationRequestTests
{
    private static readonly Guid ParticipationId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    [Fact]
    public void NewRequest_StartsPending()
    {
        JustificationRequest request = CreateRequest();

        Assert.Equal(JustificationRequestStatus.Pending, request.Status);
        Assert.Equal(50, request.Description.Length);
        Assert.Null(request.DecidedByMemberId);
    }

    [Fact]
    public void Approve_RecordsDirectorAndDate()
    {
        JustificationRequest request = CreateRequest();
        Guid directorId = Guid.NewGuid();
        DateTime decidedAt = DateTime.UtcNow;

        request.Approve(directorId, decidedAt);

        Assert.Equal(JustificationRequestStatus.Approved, request.Status);
        Assert.Equal(directorId, request.DecidedByMemberId);
        Assert.Equal(decidedAt, request.DecidedAtUtc);
    }

    [Fact]
    public void Decision_CannotBeRepeated()
    {
        JustificationRequest request = CreateRequest();
        request.Reject(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => request.Approve(Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Description_WithLessThan50Characters_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new JustificationRequest(
            ParticipationId, MemberId, JustificationReason.Health, "short",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(48)));
    }

    private static JustificationRequest CreateRequest() => new(
        ParticipationId,
        MemberId,
        JustificationReason.Health,
        new string('a', 50),
        DateTime.UtcNow,
        DateTime.UtcNow.AddHours(48));
}
