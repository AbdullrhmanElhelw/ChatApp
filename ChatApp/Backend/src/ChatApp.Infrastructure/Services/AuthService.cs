using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Auth.DTOs;
using ChatApp.Application.Features.Auth.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Identity;
using ChatApp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValidationException = ChatApp.Application.Common.Exceptions.ValidationException;

namespace ChatApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<RefreshTokenRequestDto> _refreshTokenValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        AppDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtSettings> jwtSettings,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RefreshTokenRequestDto> refreshTokenValidator,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtSettings.Value;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Validate unique Email
        var existingEmailUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmailUser != null)
        {
            throw new ValidationException("Email", $"An account with email '{request.Email}' already exists.");
        }

        // Validate unique Username
        var existingNameUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingNameUser != null)
        {
            throw new ValidationException("UserName", $"The username '{request.UserName}' is already taken.");
        }

        var newUser = new AppUser
        {
            FullName = request.FullName.Trim(),
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(newUser, request.Password);
        if (!identityResult.Succeeded)
        {
            var firstError = identityResult.Errors.FirstOrDefault()?.Description ?? "Failed to create user account.";
            throw new ValidationException("Password", firstError);
        }

        _logger.LogInformation("New user registered successfully: {UserId} ({UserName})", newUser.Id, newUser.UserName);

        // Generate Access and Refresh Tokens
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(newUser);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenGenerator.HashToken(rawRefreshToken);

        var refreshTokenEntity = new UserRefreshToken
        {
            UserId = newUser.Id,
            TokenHash = hashedRefreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.UserRefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var responseDto = new AuthResponseDto(
            newUser.Id,
            newUser.UserName!,
            newUser.Email!,
            newUser.FullName,
            accessToken,
            rawRefreshToken,
            expiresAt
        );

        return ApiResponse<AuthResponseDto>.Ok(responseDto, "User registered successfully.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var identifier = request.UserNameOrEmail.Trim();
        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for identifier '{Identifier}'", identifier);
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("Login failed: Incorrect password for user '{UserId}'", user.Id);
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // Generate Access and Refresh Tokens
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenGenerator.HashToken(rawRefreshToken);

        var refreshTokenEntity = new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.UserRefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var responseDto = new AuthResponseDto(
            user.Id,
            user.UserName!,
            user.Email!,
            user.FullName,
            accessToken,
            rawRefreshToken,
            expiresAt
        );

        return ApiResponse<AuthResponseDto>.Ok(responseDto, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _refreshTokenValidator.ValidateAsync(new RefreshTokenRequestDto(refreshToken), cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var hashedToken = _jwtTokenGenerator.HashToken(refreshToken);

        var tokenRecord = await _dbContext.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hashedToken, cancellationToken);

        if (tokenRecord == null)
        {
            _logger.LogWarning("Refresh token exchange failed: Token hash not found in database.");
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // Security check: Reuse of a Revoked Token indicates Token Theft!
        if (tokenRecord.IsRevoked)
        {
            _logger.LogCritical(
                "SECURITY ALERT: Revoked refresh token reused for user {UserId}! Revoking all active sessions for this account.",
                tokenRecord.UserId);

            var activeTokens = await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == tokenRecord.UserId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException("Security violation: A revoked refresh token was used. All active sessions have been terminated.");
        }

        if (tokenRecord.IsExpired)
        {
            _logger.LogWarning("Refresh token exchange failed: Token expired at {ExpiryDate}", tokenRecord.ExpiryDate);
            throw new UnauthorizedException("Refresh token has expired. Please sign in again.");
        }

        if (tokenRecord.User == null)
        {
            throw new UnauthorizedException("User associated with this token no longer exists.");
        }

        // Rotate token: Invalidate current token and link successor
        tokenRecord.RevokedAt = DateTime.UtcNow;

        var newRawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var newHashedRefreshToken = _jwtTokenGenerator.HashToken(newRawRefreshToken);

        tokenRecord.ReplacedByTokenHash = newHashedRefreshToken;

        var newRefreshTokenEntity = new UserRefreshToken
        {
            UserId = tokenRecord.UserId,
            TokenHash = newHashedRefreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.UserRefreshTokens.Add(newRefreshTokenEntity);

        var (newAccessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(tokenRecord.User);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated successfully for user: {UserId}", tokenRecord.UserId);

        var responseDto = new AuthResponseDto(
            tokenRecord.User.Id,
            tokenRecord.User.UserName!,
            tokenRecord.User.Email!,
            tokenRecord.User.FullName,
            newAccessToken,
            newRawRefreshToken,
            expiresAt
        );

        return ApiResponse<AuthResponseDto>.Ok(responseDto, "Token refreshed successfully.");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return ApiResponse<bool>.Ok(true, "Logged out successfully.");
        }

        var hashedToken = _jwtTokenGenerator.HashToken(refreshToken);

        var tokenRecord = await _dbContext.UserRefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hashedToken && t.RevokedAt == null, cancellationToken);

        if (tokenRecord != null)
        {
            tokenRecord.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User logged out; refresh token revoked for user: {UserId}", tokenRecord.UserId);
        }

        return ApiResponse<bool>.Ok(true, "Logged out successfully.");
    }
}
