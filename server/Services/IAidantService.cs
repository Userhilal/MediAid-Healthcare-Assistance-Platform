using MediAid.Models;

namespace MediAid.Services;

public interface IAidantService
{
    Task<Aidant?> GetAidantByUserIdAsync(string userId);
    Task<bool> UpdateAidantAsync(Aidant aidant);
    Task<bool> UpdateLocationAsync(string userId, double latitude, double longitude, double radius);
    Task<List<Aidant>> GetAllAidantsWithLocationAsync();
}


