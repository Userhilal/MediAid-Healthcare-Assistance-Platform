using MediAid.Models;

namespace MediAid.Services;

public interface IReviewService
{
    Task<Review?> CreateReviewAsync(string requestId, string aidantId, string patientId, int rating, string? comment);
    Task<List<Review>> GetReviewsByAidantIdAsync(string aidantId);
    Task<Review?> GetReviewByRequestIdAsync(string requestId);
    Task<double> CalculateReputationScoreAsync(string aidantId);
}



