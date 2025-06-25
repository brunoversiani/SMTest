using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SMTest.Domain.Entities;
using SMTest.Infrastructure.Services;
using Xunit;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        SetupMockConfiguration();
        _authService = new AuthService(_mockConfig.Object);
    }

    private void SetupMockConfiguration()
    {
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(x => x.Value).Returns("32_character_long_secret_key_1234567890");

        var issuerSectionMock = new Mock<IConfigurationSection>();
        issuerSectionMock.Setup(x => x.Value).Returns("test-issuer");

        var audienceSectionMock = new Mock<IConfigurationSection>();
        audienceSectionMock.Setup(x => x.Value).Returns("test-audience");

        var expireSectionMock = new Mock<IConfigurationSection>();
        expireSectionMock.Setup(x => x.Value).Returns("30");

        _mockConfig.Setup(x => x["Jwt:Key"]).Returns("32_character_long_secret_key_1234567890");
        _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
        _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("test-audience");
        _mockConfig.Setup(x => x["Jwt:ExpireMinutes"]).Returns("30");
    }

    [Fact]
    public void CreateToken_WithValidUser_ReturnsJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com"
        };

        // Act
        var token = _authService.CreateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwtToken.Issuer.Should().Be("test-issuer");
    }

    [Fact]
    public void CreateToken_WithInvalidKey_ThrowsException()
    {
        // Arrange
        _mockConfig.Setup(x => x["Jwt:Key"]).Returns("short_key");
        var user = new User { Id = "1", Email = "test@example.com" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _authService.CreateToken(user));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid_token")]
    public void ValidateToken_WithInvalidToken_ReturnsNull(string invalidToken)
    {
        // Act
        var result = _authService.ValidateToken(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId.ToString(), Email = "test@example.com" };
        var validToken = _authService.CreateToken(user);

        // Act
        var result = _authService.ValidateToken(validToken);

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        _mockConfig.Setup(x => x["Jwt:ExpireMinutes"]).Returns("0"); // Expired immediately
        var user = new User { Id = Guid.NewGuid().ToString(), Email = "test@example.com" };
        var expiredToken = _authService.CreateToken(user);

        // Act
        var result = _authService.ValidateToken(expiredToken);

        // Assert
        result.Should().BeNull();
    }
}