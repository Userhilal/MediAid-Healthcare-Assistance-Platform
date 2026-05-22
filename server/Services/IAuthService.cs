using MediAid.Models;

namespace MediAid.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string email, string password, string firstName, string lastName, string? phoneNumber, string role);
    Task<bool> ChangePasswordAsync(string userId, string newPassword);
    Task<string> GenerateJwtTokenAsync(User user);
    Task<User?> ValidateRefreshTokenAsync(string refreshToken);
}

