using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class ReviewService : IReviewService
{
    private readonly MongoDbContext _context;

    public ReviewService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> CreateReviewAsync(string requestId, string aidantId, string patientId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
        {
            return null;
        }

        // Check if review already exists for this request
        var existing = await _context.Reviews.Find(r => r.RequestId == requestId).FirstOrDefaultAsync();
        if (existing != null)
        {
            return null;
        }

        var review = new Review
        {
            RequestId = requestId,
            AidantId = aidantId,
            PatientId = patientId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Reviews.InsertOneAsync(review);

        // Update aidant reputation
        var reputationScore = await CalculateReputationScoreAsync(aidantId);
        var aidant = await _context.Aidants.Find(a => a.Id == aidantId).FirstOrDefaultAsync();
        if (aidant != null)
        {
            aidant.ReputationScore = reputationScore;
            aidant.UpdatedAt = DateTime.UtcNow;
            await _context.Aidants.ReplaceOneAsync(a => a.Id == aidantId, aidant);
        }

        return review;
    }

    public async Task<List<Review>> GetReviewsByAidantIdAsync(string aidantId)
    {
        return await _context.Reviews.Find(r => r.AidantId == aidantId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetReviewByRequestIdAsync(string requestId)
    {
        return await _context.Reviews.Find(r => r.RequestId == requestId).FirstOrDefaultAsync();
    }

    public async Task<double> CalculateReputationScoreAsync(string aidantId)
    {
        var reviews = await GetReviewsByAidantIdAsync(aidantId);
        if (reviews.Count == 0)
        {
            return 0.0;
        }

        var averageRating = reviews.Average(r => r.Rating);
        return Math.Round(averageRating, 2);
    }
}


