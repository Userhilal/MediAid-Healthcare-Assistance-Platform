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
    private readonly MongoDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(MongoDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null)
        {
            return null;
        }

        // Check if account is locked
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return null;
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
            }
            user.UpdatedAt = DateTime.UtcNow;
            await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
            return null;
        }

        // Reset failed login attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

        // Temporarily allow login without email verification for development
        // if (!user.IsEmailVerified)
        // {
        //     return null;
        // }

        return user;
    }

    public async Task<bool> RegisterAsync(string email, string password, string firstName, string lastName, string? phoneNumber, string role)
    {
        try
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Role = role,
                IsEmailVerified = true, // Temporarily set to true for development
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(user);

            // Create role-specific profile
            if (role == "Patient")
            {
                var patient = new Patient
                {
                    UserId = user.Id!,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.Patients.InsertOneAsync(patient);
            }
            else if (role == "Aidant")
            {
                var aidant = new Aidant
                {
                    UserId = user.Id!,
                    ReputationScore = 0,
                    TotalMissions = 0,
                    CompletedMissions = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.Aidants.InsertOneAsync(aidant);
            }

            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            // Duplicate key error - email already exists
            return false;
        }
        catch (MongoBulkWriteException<User> ex) when (ex.WriteErrors.Any(e => e.Code == 11000))
        {
            // Duplicate key error in bulk operation
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string newPassword)
    {
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);
        return result.ModifiedCount > 0;
    }

    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? "your-secret-key-at-least-32-characters-long";
        var issuer = jwtSettings["Issuer"] ?? "MediAid";
        var audience = jwtSettings["Audience"] ?? "MediAidUsers";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
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

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<User?> ValidateRefreshTokenAsync(string refreshToken)
    {
        // Implementation for refresh token validation
        // For now, return null as refresh tokens are not fully implemented
        return await Task.FromResult<User?>(null);
    }
}
