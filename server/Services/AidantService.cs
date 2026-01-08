using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class AidantService : IAidantService
{
    private readonly MongoDbContext _context;

    public AidantService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Aidant?> GetAidantByUserIdAsync(string userId)
    {
        return await _context.Aidants.Find(a => a.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAidantAsync(Aidant aidant)
    {
        aidant.UpdatedAt = DateTime.UtcNow;
        var result = await _context.Aidants.ReplaceOneAsync(a => a.Id == aidant.Id, aidant);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateLocationAsync(string userId, double latitude, double longitude, double radius)
    {
        var aidant = await GetAidantByUserIdAsync(userId);
        if (aidant == null)
        {
            return false;
        }

        aidant.Location = new Location
        {
            Type = "Point",
            Coordinates = new double[] { longitude, latitude }
        };
        aidant.InterventionRadius = radius;
        aidant.UpdatedAt = DateTime.UtcNow;

        return await UpdateAidantAsync(aidant);
    }

    public async Task<List<Aidant>> GetAllAidantsWithLocationAsync()
    {
        var filter = Builders<Aidant>.Filter.And(
            Builders<Aidant>.Filter.Ne(a => a.Location, null),
            Builders<Aidant>.Filter.Eq(a => a.AvailabilityStatus, "Available")
        );
        return await _context.Aidants.Find(filter).ToListAsync();
    }
}


