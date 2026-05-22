using BCrypt.Net;
using MediAid.Data;
using MediAid.Models;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MediAid.Services;

public class AuthService : IAuthService
{
    private static readonly string[] AllowedPublicRoles = { "Patient", "Aidant" };

    private readonly MongoDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(MongoDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);

        var user = await _context.Users
            .Find(u => u.Email == normalizedEmail)
            .FirstOrDefaultAsync();

        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

            return null;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

        return user;
    }

    public async Task<bool> RegisterAsync(string email, string password, string firstName, string lastName, string? phoneNumber, string role)
    {
        try
        {
            var normalizedEmail = NormalizeEmail(email);
            var normalizedRole = NormalizeRole(role);

            if (Array.IndexOf(AllowedPublicRoles, normalizedRole) < 0)
            {
                return false;
            }

            var existing = await _context.Users
                .Find(u => u.Email == normalizedEmail)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return false;
            }

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = firstName?.Trim(),
                LastName = lastName?.Trim(),
                PhoneNumber = phoneNumber?.Trim(),
                Role = normalizedRole,
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(user);

            if (normalizedRole == "Patient")
            {
                var patient = new Patient
                {
                    UserId = user.Id!,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Patients.InsertOneAsync(patient);
            }

            if (normalizedRole == "Aidant")
            {
                var aidant = new Aidant
                {
                    UserId = user.Id!,
                    ReputationScore = 0,
                    TotalMissions = 0,
                    CompletedMissions = 0,
                    AvailabilityStatus = "Available",
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Aidants.InsertOneAsync(aidant);
            }

            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            return false;
        }
        catch (MongoBulkWriteException<User> ex) when (ex.WriteErrors.Any(e => e.Code == 11000))
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string newPassword)
    {
        var user = await _context.Users.Find(u => u.Id == userId && u.IsActive).FirstOrDefaultAsync();

        if (user == null)
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.LockoutEnd = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);

        return result.ModifiedCount > 0;
    }

    public Task<string> GenerateJwtTokenAsync(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var secretKey = jwtSettings["SecretKey"] ?? "DevelopmentSecretKeyForMediAidMustBeAtLeast32CharactersLong";
        var issuer = jwtSettings["Issuer"] ?? "MediAid";
        var audience = jwtSettings["Audience"] ?? "MediAidUsers";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public Task<User?> ValidateRefreshTokenAsync(string refreshToken)
    {
        return Task.FromResult<User?>(null);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeRole(string role)
    {
        return role?.Trim() switch
        {
            "Patient" => "Patient",
            "Aidant" => "Aidant",
            _ => string.Empty
        };
    }
}

