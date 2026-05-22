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

    public ProposalService(
        MongoDbContext context,
        IRequestService requestService,
        INotificationService notificationService,
        IPlanningService? planningService = null)
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

        if (request.RequiresExpertValidation && !request.IsExpertValidated)
        {
            return null;
        }

        var aidant = await _context.Aidants.Find(a => a.Id == aidantId).FirstOrDefaultAsync();
        if (aidant == null || aidant.AvailabilityStatus == "Unavailable")
        {
            return null;
        }

        var existing = await _context.Proposals
            .Find(p => p.RequestId == requestId && p.AidantId == aidantId && p.Status == "Pending")
            .FirstOrDefaultAsync();

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

        await _notificationService.CreateNotificationAsync(
            request.PatientId,
            "NewProposal",
            "Nouvelle proposition",
            $"Un aidant a fait une proposition pour votre demande : {request.Title}",
            proposal.Id,
            "Proposal");

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
        if (proposal == null || proposal.Status != "Pending")
        {
            return false;
        }

        var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
        if (request == null || request.PatientId != patientId || request.Status != "Open")
        {
            return false;
        }

        var aidant = await _context.Aidants.Find(a => a.Id == proposal.AidantId).FirstOrDefaultAsync();
        if (aidant == null || string.IsNullOrWhiteSpace(aidant.UserId))
        {
            return false;
        }

        proposal.Status = "Accepted";
        proposal.RespondedAt = DateTime.UtcNow;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _context.Proposals.ReplaceOneAsync(p => p.Id == proposal.Id, proposal);
        await _requestService.AssignAidantAsync(request.Id!, proposal.AidantId);

        var otherProposals = await _context.Proposals
            .Find(p => p.RequestId == proposal.RequestId && p.Id != proposal.Id && p.Status == "Pending")
            .ToListAsync();

        foreach (var other in otherProposals)
        {
            other.Status = "Rejected";
            other.RespondedAt = DateTime.UtcNow;
            other.UpdatedAt = DateTime.UtcNow;

            await _context.Proposals.ReplaceOneAsync(p => p.Id == other.Id, other);

            var otherAidant = await _context.Aidants.Find(a => a.Id == other.AidantId).FirstOrDefaultAsync();
            if (otherAidant != null && !string.IsNullOrWhiteSpace(otherAidant.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    otherAidant.UserId,
                    "ProposalRejected",
                    "Proposition refusée",
                    $"Une autre proposition a été acceptée pour « {request.Title} ».",
                    request.Id,
                    "Request");
            }
        }

        if (proposal.EstimatedArrivalTime.HasValue && _planningService != null)
        {
            try
            {
                var missionDate = proposal.EstimatedArrivalTime.Value.Date;
                var startTime = proposal.EstimatedArrivalTime.Value.TimeOfDay;
                var endTime = startTime.Add(TimeSpan.FromHours(1));

                await _planningService.AssignMissionToSlotAsync(
                    proposal.AidantId,
                    missionDate,
                    startTime,
                    endTime,
                    request.Id!,
                    request.Title);
            }
            catch
            {
                // Planning should not block the main acceptance workflow.
            }
        }

        await _notificationService.CreateNotificationAsync(
            aidant.UserId,
            "ProposalAccepted",
            "Proposition acceptée",
            $"Votre proposition pour « {request.Title} » a été acceptée.",
            request.Id,
            "Request");

        return true;
    }

    public async Task<bool> RejectProposalAsync(string proposalId, string patientId)
    {
        var proposal = await GetProposalByIdAsync(proposalId);
        if (proposal == null || proposal.Status != "Pending")
        {
            return false;
        }

        var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
        if (request == null || request.PatientId != patientId)
        {
            return false;
        }

        var aidant = await _context.Aidants.Find(a => a.Id == proposal.AidantId).FirstOrDefaultAsync();

        proposal.Status = "Rejected";
        proposal.RespondedAt = DateTime.UtcNow;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _context.Proposals.ReplaceOneAsync(p => p.Id == proposal.Id, proposal);

        if (aidant != null && !string.IsNullOrWhiteSpace(aidant.UserId))
        {
            await _notificationService.CreateNotificationAsync(
                aidant.UserId,
                "ProposalRejected",
                "Proposition refusée",
                $"Votre proposition pour « {request.Title} » a été refusée.",
                request.Id,
                "Request");
        }

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
