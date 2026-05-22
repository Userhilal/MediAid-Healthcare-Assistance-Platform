using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class UserService : IUserService
{
    private readonly MongoDbContext _context;

    public UserService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Users.Find(u => u.Email == normalizedEmail).FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
        return result.ModifiedCount > 0;
    }

    public async Task<Patient?> GetPatientByUserIdAsync(string userId)
    {
        return await _context.Patients.Find(p => p.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<Aidant?> GetAidantByUserIdAsync(string userId)
    {
        return await _context.Aidants.Find(a => a.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<Expert?> GetExpertByUserIdAsync(string userId)
    {
        return await _context.Experts.Find(e => e.UserId == userId).FirstOrDefaultAsync();
    }
}




