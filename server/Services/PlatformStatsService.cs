using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class PlatformStatsService : IPlatformStatsService
{
    private readonly MongoDbContext _context;

    public PlatformStatsService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformStats> GetStatsAsync()
    {
        var stats = new PlatformStats();

        try
        {
            await _context.CanConnectAsync();

            stats.TotalUsers = (int)await _context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty);
            stats.TotalPatients = (int)await _context.Patients.CountDocumentsAsync(FilterDefinition<Patient>.Empty);
            stats.TotalAidants = (int)await _context.Aidants.CountDocumentsAsync(FilterDefinition<Aidant>.Empty);
            stats.AvailableAidants = (int)await _context.Aidants.CountDocumentsAsync(
                Builders<Aidant>.Filter.Eq(a => a.AvailabilityStatus, "Available"));

            stats.OpenRequests = (int)await _context.Requests.CountDocumentsAsync(
                Builders<Request>.Filter.Eq(r => r.Status, "Open"));

            stats.CompletedRequests = (int)await _context.Requests.CountDocumentsAsync(
                Builders<Request>.Filter.Eq(r => r.Status, "Completed"));

            var reviews = await _context.Reviews.Find(FilterDefinition<Review>.Empty).ToListAsync();
            stats.AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            var completedRequests = await _context.Requests
                .Find(Builders<Request>.Filter.Eq(r => r.Status, "Completed"))
                .ToListAsync();

            stats.PatientsHelped = completedRequests
                .Select(r => r.PatientId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .Count();

            var hoursFromRequests = completedRequests.Sum(request =>
            {
                if (!request.RequestedDate.HasValue)
                {
                    return 1.0;
                }

                var end = request.CompletedAt ?? request.UpdatedAt;
                var duration = end - request.RequestedDate.Value;

                return duration.TotalHours > 0 && duration.TotalHours < 24
                    ? duration.TotalHours
                    : 1.0;
            });

            var aidants = await _context.Aidants.Find(FilterDefinition<Aidant>.Empty).ToListAsync();
            var hoursFromAidants = aidants.Sum(a => a.TotalHours);

            stats.TotalVolunteerHours = (int)Math.Round(Math.Max(hoursFromRequests, hoursFromAidants));
        }
        catch
        {
            stats.IsDatabaseAvailable = false;
        }

        return stats;
    }
}

