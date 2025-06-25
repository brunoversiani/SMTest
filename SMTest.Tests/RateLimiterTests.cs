using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SMTest.Domain.Entities;
using SMTest.Domain.ValueObjects;
using SMTest.Infrastructure.Data;
using SMTest.Infrastructure.Services;
using Xunit;

public class RateLimiterTests
{
    private readonly Mock<AppDbContext> _mockDbContext;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly RateLimiter _rateLimiter;

    public RateLimiterTests()
    {
        _mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _mockConfig = new Mock<IConfiguration>();

        SetupMockConfiguration();
        SetupMockDbSets();

        _rateLimiter = new RateLimiter(_mockDbContext.Object, _mockConfig.Object);
    }

    private void SetupMockConfiguration()
    {
        _mockConfig.Setup(x => x["RateLimiting:MaxUrlsPerDay"]).Returns("5");
        _mockConfig.Setup(x => x["RateLimiting:MaxHitsPerMinute"]).Returns("10");
    }

    private void SetupMockDbSets()
    {
        var userDailyLimits = new List<DailyLimit>().AsQueryable();
        var rateLimitRecords = new List<RateLimitRecord>().AsQueryable();

        var mockUserLimits = new Mock<DbSet<DailyLimit>>();
        var mockRateRecords = new Mock<DbSet<RateLimitRecord>>();

        mockUserLimits.As<IQueryable<DailyLimit>>().Setup(m => m.Provider).Returns(userDailyLimits.Provider);
        mockUserLimits.As<IQueryable<DailyLimit>>().Setup(m => m.Expression).Returns(userDailyLimits.Expression);
        mockUserLimits.As<IQueryable<DailyLimit>>().Setup(m => m.ElementType).Returns(userDailyLimits.ElementType);
        mockUserLimits.As<IQueryable<DailyLimit>>().Setup(m => m.GetEnumerator()).Returns(userDailyLimits.GetEnumerator());

        mockRateRecords.As<IQueryable<RateLimitRecord>>().Setup(m => m.Provider).Returns(rateLimitRecords.Provider);
        mockRateRecords.As<IQueryable<RateLimitRecord>>().Setup(m => m.Expression).Returns(rateLimitRecords.Expression);
        mockRateRecords.As<IQueryable<RateLimitRecord>>().Setup(m => m.ElementType).Returns(rateLimitRecords.ElementType);
        mockRateRecords.As<IQueryable<RateLimitRecord>>().Setup(m => m.GetEnumerator()).Returns(rateLimitRecords.GetEnumerator());

        _mockDbContext.Setup(x => x.UserDailyLimits).Returns(mockUserLimits.Object);
        _mockDbContext.Setup(x => x.RateLimitRecords).Returns(mockRateRecords.Object);
    }

    [Fact]
    public async Task CanCreateUrl_NewUser_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = "user1" };

        // Act
        var result = await _rateLimiter.CanCreateUrl(user);

        // Assert
        result.Should().BeTrue();
        _mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CanCreateUrl_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = "user1" };
        var existingLimit = new DailyLimit
        {
            UserId = user.Id,
            Count = 3,
            ResetDate = DateTime.UtcNow.Date
        };

        var mockSet = new Mock<DbSet<DailyLimit>>();
        mockSet.Setup(x => x.FindAsync(user.Id)).ReturnsAsync(existingLimit);
        _mockDbContext.Setup(x => x.UserDailyLimits).Returns(mockSet.Object);

        // Act
        var result = await _rateLimiter.CanCreateUrl(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanCreateUrl_OverLimit_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = "user1" };
        var existingLimit = new DailyLimit
        {
            UserId = user.Id,
            Count = 5,
            ResetDate = DateTime.UtcNow.Date
        };

        var mockSet = new Mock<DbSet<DailyLimit>>();
        mockSet.Setup(x => x.FindAsync(user.Id)).ReturnsAsync(existingLimit);
        _mockDbContext.Setup(x => x.UserDailyLimits).Returns(mockSet.Object);

        // Act
        var result = await _rateLimiter.CanCreateUrl(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanCreateUrl_NewDay_ResetsCounter()
    {
        // Arrange
        var user = new User { Id = "user1" };
        var existingLimit = new DailyLimit
        {
            UserId = user.Id,
            Count = 5,
            ResetDate = DateTime.UtcNow.Date.AddDays(-1) // Yesterday
        };

        var mockSet = new Mock<DbSet<DailyLimit>>();
        mockSet.Setup(x => x.FindAsync(user.Id)).ReturnsAsync(existingLimit);
        _mockDbContext.Setup(x => x.UserDailyLimits).Returns(mockSet.Object);

        // Act
        var result = await _rateLimiter.CanCreateUrl(user);

        // Assert
        result.Should().BeTrue();
        existingLimit.Count.Should().Be(0);
        existingLimit.ResetDate.Should().Be(DateTime.UtcNow.Date);
        _mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CanAccessUrl_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var shortCode = "abc123";
        var mockSet = new Mock<DbSet<RateLimitRecord>>();
        mockSet.Setup(x => x.CountAsync(It.IsAny<Expression<Func<RateLimitRecord, bool>>>(), default))
               .ReturnsAsync(5); // 5 accesses in last minute

        _mockDbContext.Setup(x => x.RateLimitRecords).Returns(mockSet.Object);

        // Act
        var result = await _rateLimiter.CanAccessUrl(shortCode);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RecordUrlCreation_IncrementsCounter()
    {
        // Arrange
        var userId = "user1";
        var existingLimit = new DailyLimit
        {
            UserId = userId,
            Count = 1,
            ResetDate = DateTime.UtcNow.Date
        };

        var mockSet = new Mock<DbSet<DailyLimit>>();
        mockSet.Setup(x => x.FindAsync(userId)).ReturnsAsync(existingLimit);
        _mockDbContext.Setup(x => x.UserDailyLimits).Returns(mockSet.Object);

        // Act
        await _rateLimiter.RecordUrlCreation(userId);

        // Assert
        existingLimit.Count.Should().Be(2);
        _mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }
}