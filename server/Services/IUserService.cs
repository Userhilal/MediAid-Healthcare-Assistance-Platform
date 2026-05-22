using MediAid.Models;

namespace MediAid.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> UpdateUserAsync(User user);
    Task<Patient?> GetPatientByUserIdAsync(string userId);
    Task<Aidant?> GetAidantByUserIdAsync(string userId);
    Task<Expert?> GetExpertByUserIdAsync(string userId);
}



