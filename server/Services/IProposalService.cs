using MediAid.Models;

namespace MediAid.Services;

public interface IProposalService
{
    Task<Proposal?> CreateProposalAsync(string requestId, string aidantId, string? message, DateTime? estimatedArrivalTime);
    Task<Proposal?> GetProposalByIdAsync(string proposalId);
    Task<List<Proposal>> GetProposalsByRequestIdAsync(string requestId);
    Task<List<Proposal>> GetProposalsByAidantIdAsync(string aidantId);
    Task<bool> AcceptProposalAsync(string proposalId, string patientId);
    Task<bool> RejectProposalAsync(string proposalId, string patientId);
    Task<bool> CancelProposalAsync(string proposalId, string aidantId);
}


