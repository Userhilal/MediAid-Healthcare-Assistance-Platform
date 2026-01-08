using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class ProposalService : IProposalService
{
    private readonly MongoDbContext _context;
    private readonly IRequestService _requestService;
    private readonly INotificationService _notificationService;
    private readonly IPlanningService? _planningService;

    public ProposalService(MongoDbContext context, IRequestService requestService, INotificationService notificationService, IPlanningService? planningService = null)
    {
        _context = context;
        _requestService = requestService;
        _notificationService = notificationService;
        _planningService = planningService;
    }

    public async Task<Proposal?> CreateProposalAsync(string requestId, string aidantId, string? message, DateTime? estimatedArrivalTime)
    {
        var request = await _requestService.GetRequestByIdAsync(requestId);
        if (request == null || request.Status != "Open")
        {
            return null;
        }

        // Check if proposal already exists
        var existing = await _context.Proposals.Find(p => p.RequestId == requestId && p.AidantId == aidantId && p.Status == "Pending").FirstOrDefaultAsync();
        if (existing != null)
        {
            return null;
        }

        var proposal = new Proposal
        {
            RequestId = requestId,
            AidantId = aidantId,
            Message = message,
            EstimatedArrivalTime = estimatedArrivalTime,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Proposals.InsertOneAsync(proposal);

        // Notify patient
        await _notificationService.CreateNotificationAsync(request.PatientId, "NewProposal",
            "Nouvelle proposition", $"Un aidant a fait une proposition pour votre demande: {request.Title}");

        return proposal;
    }

    public async Task<Proposal?> GetProposalByIdAsync(string proposalId)
    {
        return await _context.Proposals.Find(p => p.Id == proposalId).FirstOrDefaultAsync();
    }

    public async Task<List<Proposal>> GetProposalsByRequestIdAsync(string requestId)
    {
        return await _context.Proposals.Find(p => p.RequestId == requestId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Proposal>> GetProposalsByAidantIdAsync(string aidantId)
    {
        return await _context.Proposals.Find(p => p.AidantId == aidantId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AcceptProposalAsync(string proposalId, string patientId)
    {
        var proposal = await GetProposalByIdAsync(proposalId);
        if (proposal == null)
        {
            return false;
        }

        var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
        if (request == null || request.PatientId != patientId)
        {
            return false;
        }

        proposal.Status = "Accepted";
        proposal.RespondedAt = DateTime.UtcNow;
        proposal.UpdatedAt = DateTime.UtcNow;
        await _context.Proposals.ReplaceOneAsync(p => p.Id == proposal.Id, proposal);

        // Assign aidant to request
        await _requestService.AssignAidantAsync(request.Id!, proposal.AidantId);

        // Auto-add mission to planning if EstimatedArrivalTime is set
        if (proposal.EstimatedArrivalTime.HasValue && request.RequestedDate.HasValue && _planningService != null)
        {
            try
            {
                var missionDate = proposal.EstimatedArrivalTime.Value.Date;
                var startTime = proposal.EstimatedArrivalTime.Value.TimeOfDay;
                var endTime = startTime.Add(TimeSpan.FromHours(1)); // Default 1 hour mission
                
                await _planningService.AssignMissionToSlotAsync(
                    proposal.AidantId,
                    missionDate,
                    startTime,
                    endTime,
                    request.Id!,
                    request.Title
                );
            }
            catch
            {
                // Planning service might not be available, continue anyway
            }
        }

        // Reject other pending proposals
        var otherProposals = await _context.Proposals.Find(p => p.RequestId == proposal.RequestId && p.Id != proposal.Id && p.Status == "Pending").ToListAsync();
        foreach (var other in otherProposals)
        {
            other.Status = "Rejected";
            other.RespondedAt = DateTime.UtcNow;
            other.UpdatedAt = DateTime.UtcNow;
            await _context.Proposals.ReplaceOneAsync(p => p.Id == other.Id, other);
        }

        // Notify aidant
        await _notificationService.CreateNotificationAsync(proposal.AidantId, "ProposalAccepted",
            "Proposition acceptée", $"Votre proposition pour '{request.Title}' a été acceptée.");

        return true;
    }

    public async Task<bool> RejectProposalAsync(string proposalId, string patientId)
    {
        var proposal = await GetProposalByIdAsync(proposalId);
        if (proposal == null)
        {
            return false;
        }

        var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
        if (request == null || request.PatientId != patientId)
        {
            return false;
        }

        proposal.Status = "Rejected";
        proposal.RespondedAt = DateTime.UtcNow;
        proposal.UpdatedAt = DateTime.UtcNow;
        await _context.Proposals.ReplaceOneAsync(p => p.Id == proposal.Id, proposal);

        // Notify aidant
        await _notificationService.CreateNotificationAsync(proposal.AidantId, "ProposalRejected",
            "Proposition refusée", $"Votre proposition pour '{request.Title}' a été refusée.");

        return true;
    }

    public async Task<bool> CancelProposalAsync(string proposalId, string aidantId)
    {
        var proposal = await GetProposalByIdAsync(proposalId);
        if (proposal == null || proposal.AidantId != aidantId || proposal.Status != "Pending")
        {
            return false;
        }

        proposal.Status = "Cancelled";
        proposal.UpdatedAt = DateTime.UtcNow;
        await _context.Proposals.ReplaceOneAsync(p => p.Id == proposal.Id, proposal);
        return true;
    }
}


