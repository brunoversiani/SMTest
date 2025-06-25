using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SMTest.Domain.Entities;
using SMTest.Domain.Interfaces;
using SMTest.Domain.ValueObjects;
using SMTest.Infrastructure.Data;

namespace SMTest.Infrastructure.Services
{
    public class RateLimiter : IRateLimiter
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _config;

        public RateLimiter(AppDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _config = config;
        }

        private int MaxUrlsPerDay => int.Parse(_config["RateLimiting:MaxUrlsPerDay"] ?? "5");
        private int MaxHitsPerMinute => int.Parse(_config["RateLimiting:MaxHitsPerMinute"] ?? "10");

        public async Task<bool> CanCreateUrl(User user)
        {
            var limit = await _dbContext.UserDailyLimits
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Reset if it's a new day
            if (limit == null || DateTime.UtcNow.Date > limit.ResetDate.Date)
            {
                if (limit == null)
                {
                    limit = new DailyLimit
                    {
                        UserId = user.Id,
                        ResetDate = DateTime.UtcNow.Date
                    };
                    _dbContext.UserDailyLimits.Add(limit);
                }
                else
                {
                    limit.ResetDate = DateTime.UtcNow.Date;
                    limit.Count = 0;
                }
                await _dbContext.SaveChangesAsync();
            }

            return limit.Count < MaxUrlsPerDay;
        }

        public async Task<bool> CanAccessUrl(string shortCode)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var accessCount = await _dbContext.RateLimitRecords
                .CountAsync(r => r.ShortCode == $"url:{shortCode}" &&
                               r.Timestamp >= oneMinuteAgo);
            return accessCount < MaxHitsPerMinute;
        }

        public async Task RecordUrlAccess(string shortCode)
        {
            _dbContext.RateLimitRecords.Add(new RateLimitRecord
            {
                ShortCode = $"url:{shortCode}",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task RecordUrlCreation(string userId)
        {
            var limit = await _dbContext.UserDailyLimits
                .FirstOrDefaultAsync(l => l.UserId == userId);
            if (limit != null)
            {
                limit.Count++;
                await _dbContext.SaveChangesAsync();
            }
        }
    }

}
