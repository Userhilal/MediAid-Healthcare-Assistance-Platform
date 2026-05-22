using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class ProposalRecommendationService : IProposalRecommendationService
{
    private readonly MongoDbContext _context;
    private readonly IReviewService _reviewService;

    public ProposalRecommendationService(MongoDbContext context, IReviewService reviewService)
    {
        _context = context;
        _reviewService = reviewService;
    }

    public async Task<List<ProposalRecommendation>> GetRecommendationsForProposalsAsync(string requestId, List<Proposal> proposals)
    {
        var request = await _context.Requests.Find(r => r.Id == requestId).FirstOrDefaultAsync();
        if (request == null)
        {
            return new List<ProposalRecommendation>();
        }

        var recommendations = new List<ProposalRecommendation>();

        foreach (var proposal in proposals)
        {
            var aidant = await _context.Aidants.Find(a => a.Id == proposal.AidantId).FirstOrDefaultAsync();
            var aidantUser = aidant != null ? await _context.Users.Find(u => u.Id == aidant.UserId).FirstOrDefaultAsync() : null;

            var recommendation = new ProposalRecommendation
            {
                Proposal = proposal,
                Aidant = aidant,
                AidantUser = aidantUser
            };

            // Calcul du score de recommandation
            double score = 0.0;
            var reasons = new List<string>();

            // Score basé sur la réputation (0-50 points)
            if (aidant != null)
            {
                score += aidant.ReputationScore * 10; // 5 étoiles = 50 points
                if (aidant.ReputationScore >= 4.5)
                {
                    reasons.Add("Excellente réputation");
                }
            }

            // Score basé sur le nombre de missions complétées (0-30 points)
            if (aidant != null)
            {
                var completedRatio = aidant.TotalMissions > 0 
                    ? (double)aidant.CompletedMissions / aidant.TotalMissions 
                    : 0;
                score += completedRatio * 30;
                if (completedRatio >= 0.9 && aidant.CompletedMissions > 10)
                {
                    reasons.Add("Taux de réussite élevé");
                }
            }

            // Score basé sur la distance (0-20 points)
            if (request.Location != null && aidant?.Location != null)
            {
                var distance = CalculateDistance(
                    request.Location.Coordinates[1], request.Location.Coordinates[0],
                    aidant.Location.Coordinates[1], aidant.Location.Coordinates[0]
                );
                if (distance <= 2) score += 20;
                else if (distance <= 5) score += 15;
                else if (distance <= 10) score += 10;
                else score += 5;
                
                if (distance <= 2)
                {
                    reasons.Add("Très proche");
                }
            }

            recommendation.RecommendationScore = score;
            recommendation.IsRecommended = score >= 70; // Seuil de recommandation
            recommendation.RecommendationReason = reasons.Any() ? string.Join(", ", reasons) : null;

            recommendations.Add(recommendation);
        }

        // Trier par score décroissant
        return recommendations.OrderByDescending(r => r.RecommendationScore).ToList();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in kilometers
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}


