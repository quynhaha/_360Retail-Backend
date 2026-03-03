using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using _360Retail.Services.Identity.Infrastructure.Services;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Shared.Common.Exceptions;

namespace Identity.Auth.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly IdentityDbContext _db;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IPasswordHasher<AppUser>> _passwordHasher;
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly AuthService _sut; // System Under Test

    public AuthServiceTests()
    {
        // Use InMemory DB — skip PostgreSQL-specific features
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: $"IdentityTestDb_{Guid.NewGuid()}")
            .Options;

        _db = new IdentityDbContext(options);
        _emailService = new Mock<IEmailService>();
        _passwordHasher = new Mock<IPasswordHasher<AppUser>>();
        _httpClientFactory = new Mock<IHttpClientFactory>();

        var configData = new Dictionary<string, string?>
        {
            { "JwtSettings:Key", "THIS_IS_A_VERY_LONG_AND_SECURE_SECRET_KEY_360RETAIL_FOR_TESTING" },
            { "JwtSettings:Issuer", "360Retail_Identity" },
            { "JwtSettings:Audience", "360Retail_Users" },
            { "JwtSettings:ExpireMinutes", "120" }
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _sut = new AuthService(_db, _config, _emailService.Object, _passwordHasher.Object, _httpClientFactory.Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // ============ REGISTER ============

    [Fact]
    public async Task RegisterAsync_ValidInput_CreatesUserWithPotentialOwnerRole()
    {
        // Arrange
        _passwordHasher
            .Setup(x => x.HashPassword(It.IsAny<AppUser>(), It.IsAny<string>()))
            .Returns("hashed_password_123");

        var dto = new RegisterUserDto("test@example.com", "Password123!", "Test User", null);

        // Act
        await _sut.RegisterAsync(dto);

        // Assert
        var user = await _db.AppUsers.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(user);
        Assert.Equal("Registered", user.Status);
        Assert.False(user.IsActivated); // OTP verification required
        Assert.Equal("hashed_password_123", user.PasswordHash);
        Assert.Contains(user.Roles, r => r.RoleName == "PotentialOwner");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsException()
    {
        // Arrange — seed existing user
        _db.AppUsers.Add(new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            UserName = "Existing",
            PasswordHash = "hash",
            Status = "Registered",
            IsActivated = true
        });
        await _db.SaveChangesAsync();

        _passwordHasher
            .Setup(x => x.HashPassword(It.IsAny<AppUser>(), It.IsAny<string>()))
            .Returns("hashed");

        var dto = new RegisterUserDto("existing@example.com", "Password123!");

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() => _sut.RegisterAsync(dto));
    }

    // ============ LOGIN ============

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResult()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            UserName = "user@test.com",
            PasswordHash = "hashed_pass",
            Status = "Registered",
            IsActivated = true
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _passwordHasher
            .Setup(x => x.VerifyHashedPassword(It.IsAny<AppUser>(), "hashed_pass", "Password123!"))
            .Returns(PasswordVerificationResult.Success);

        var dto = new LoginDto("user@test.com", "Password123!");

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            UserName = "user@test.com",
            PasswordHash = "hashed_pass",
            Status = "Registered",
            IsActivated = true
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _passwordHasher
            .Setup(x => x.VerifyHashedPassword(It.IsAny<AppUser>(), "hashed_pass", "wrong_password"))
            .Returns(PasswordVerificationResult.Failed);

        var dto = new LoginDto("user@test.com", "wrong_password");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto("nobody@test.com", "Password123!");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(dto));
    }

    // ============ CHANGE PASSWORD ============

    [Fact]
    public async Task ChangePasswordAsync_IncorrectCurrentPassword_ThrowsException()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            UserName = "Test",
            PasswordHash = "current_hash",
            Status = "Active",
            IsActivated = true
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _passwordHasher
            .Setup(x => x.VerifyHashedPassword(It.IsAny<AppUser>(), "current_hash", "wrong_old"))
            .Returns(PasswordVerificationResult.Failed);

        var dto = new ChangePasswordRequest { CurrentPassword = "wrong_old", NewPassword = "NewPass123!", ConfirmNewPassword = "NewPass123!" };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() => _sut.ChangePasswordAsync(user.Id, dto));
    }

    // ============ FORGOT PASSWORD ============

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_SendsEmail()
    {
        // Arrange
        _db.AppUsers.Add(new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "forgot@test.com",
            UserName = "Forgot User",
            PasswordHash = "hash",
            Status = "Active",
            IsActivated = true
        });
        await _db.SaveChangesAsync();

        // Act
        await _sut.ForgotPasswordAsync("forgot@test.com");

        // Assert — email should have been sent
        _emailService.Verify(
            x => x.SendForgotPasswordEmailAsync("forgot@test.com", It.IsAny<string>(), It.IsAny<string>(), 15),
            Times.Once
        );

        // Assert — reset code should be stored
        var user = await _db.AppUsers.FirstAsync(u => u.Email == "forgot@test.com");
        Assert.NotNull(user.PasswordResetCode);
        Assert.NotNull(user.PasswordResetExpiry);
        Assert.True(user.PasswordResetExpiry > DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPasswordAsync_NonExistentEmail_DoesNotThrow()
    {
        // Act — should silently succeed (security: don't reveal if email exists)
        await _sut.ForgotPasswordAsync("nobody@test.com");

        // Assert — no email sent
        _emailService.Verify(
            x => x.SendForgotPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never
        );
    }

    // ============ RESET PASSWORD ============

    [Fact]
    public async Task ResetPasswordAsync_InvalidCode_ThrowsException()
    {
        // Arrange
        _db.AppUsers.Add(new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "reset@test.com",
            UserName = "Reset User",
            PasswordHash = "hash",
            Status = "Active",
            IsActivated = true,
            PasswordResetCode = "123456",
            PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15)
        });
        await _db.SaveChangesAsync();

        // Act & Assert — wrong code
        await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ResetPasswordAsync("reset@test.com", "999999", "NewPass123!")
        );
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredCode_ThrowsException()
    {
        // Arrange
        _db.AppUsers.Add(new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "expired@test.com",
            UserName = "Expired User",
            PasswordHash = "hash",
            Status = "Active",
            IsActivated = true,
            PasswordResetCode = "123456",
            PasswordResetExpiry = DateTime.UtcNow.AddMinutes(-5) // Already expired
        });
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ResetPasswordAsync("expired@test.com", "123456", "NewPass123!")
        );
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidCode_UpdatesPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _db.AppUsers.Add(new AppUser
        {
            Id = userId,
            Email = "valid@test.com",
            UserName = "Valid User",
            PasswordHash = "old_hash",
            Status = "Active",
            IsActivated = true,
            PasswordResetCode = "123456",
            PasswordResetExpiry = DateTime.UtcNow.AddMinutes(10),
            MustChangePassword = true
        });
        await _db.SaveChangesAsync();

        _passwordHasher
            .Setup(x => x.HashPassword(It.IsAny<AppUser>(), "NewSecurePass123!"))
            .Returns("new_hashed_password");

        // Act
        await _sut.ResetPasswordAsync("valid@test.com", "123456", "NewSecurePass123!");

        // Assert
        var user = await _db.AppUsers.FirstAsync(u => u.Id == userId);
        Assert.Equal("new_hashed_password", user.PasswordHash);
        Assert.Null(user.PasswordResetCode);
        Assert.Null(user.PasswordResetExpiry);
        Assert.False(user.MustChangePassword);
    }
}
