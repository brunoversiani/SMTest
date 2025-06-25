using SMTest.Domain.Entities;

namespace SMTest.Domain.Interfaces
{
    public interface IRateLimiter
    {
        Task<bool> CanCreateUrl(User user);
        Task<bool> CanAccessUrl(string shortCode);
        Task RecordUrlAccess(string shortCode);
        Task RecordUrlCreation(string userId);
    }

}
