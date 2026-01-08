using MediAid.Models;

namespace MediAid.Services;

public interface IProposalRecommendationService
{
    Task<List<ProposalRecommendation>> GetRecommendationsForProposalsAsync(string requestId, List<Proposal> proposals);
}

public class ProposalRecommendation
{
    public Proposal Proposal { get; set; } = null!;
    public Aidant? Aidant { get; set; }
    public User? AidantUser { get; set; }
    public double RecommendationScore { get; set; }
    public bool IsRecommended { get; set; }
    public string? RecommendationReason { get; set; }
}

