using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public interface IIntelligentAlertService
{
    Task<List<IntelligentAlert>> GetAlertsForPatientAsync(string userId);
    Task CheckAndCreateAlertsAsync(string requestId);
}

public class IntelligentAlertService : IIntelligentAlertService
{
    private readonly MongoDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IRequestService _requestService;
    private readonly IProposalService _proposalService;

    public IntelligentAlertService(MongoDbContext context, INotificationService notificationService,
        IRequestService requestService, IProposalService proposalService)
    {
        _context = context;
        _notificationService = notificationService;
        _requestService = requestService;
        _proposalService = proposalService;
    }

    public async Task<List<IntelligentAlert>> GetAlertsForPatientAsync(string userId)
    {
        var alerts = new List<IntelligentAlert>();
        var requests = await _requestService.GetRequestsByPatientIdAsync(userId);

        foreach (var request in requests.Where(r => r.Status == "Open" || r.Status == "Assigned" || r.Status == "InProgress"))
        {
            // VÃ©rifier si un aidant est proche
            if (request.Status == "Assigned" || request.Status == "InProgress")
            {
                var allProposalsForRequest = await _proposalService.GetProposalsByRequestIdAsync(request.Id!);
                var acceptedProposal = allProposalsForRequest.FirstOrDefault(p => p.Status == "Accepted");
                
                if (acceptedProposal != null && request.Location != null)
                {
                    var aidant = await _context.Aidants.Find(a => a.Id == acceptedProposal.AidantId).FirstOrDefaultAsync();
                    if (aidant?.Location != null)
                    {
                        var distance = CalculateDistance(
                            request.Location.Coordinates[1], request.Location.Coordinates[0],
                            aidant.Location.Coordinates[1], aidant.Location.Coordinates[0]
                        );

                        if (distance <= 2)
                        {
                            alerts.Add(new IntelligentAlert
                            {
                                Type = "AidantProche",
                                Title = "Un aidant est proche",
                                Message = $"L'aidant assignÃ© est Ã  moins de {distance:F1} km de votre localisation",
                                Priority = "High",
                                RequestId = request.Id!,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            // VÃ©rifier si la demande est prioritaire
            if (request.Urgency == "Critical" || request.Urgency == "High")
            {
                var timeSinceCreation = DateTime.UtcNow - request.CreatedAt;
                if (timeSinceCreation.TotalHours > 2 && request.Status == "Open")
                {
                    alerts.Add(new IntelligentAlert
                    {
                        Type = "DemandePrioritaire",
                        Title = "Votre demande est prioritaire",
                        Message = "Votre demande urgente nÃ©cessite une attention immÃ©diate",
                        Priority = "Critical",
                        RequestId = request.Id!,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // VÃ©rifier les nouvelles propositions
            var allProposals = await _proposalService.GetProposalsByRequestIdAsync(request.Id!);
            var unreadProposals = allProposals.Where(p => p.Status == "Pending").Count();
            if (unreadProposals > 0)
            {
                alerts.Add(new IntelligentAlert
                {
                    Type = "NouvellesPropositions",
                    Title = "Nouvelles propositions",
                    Message = $"Vous avez {unreadProposals} nouvelle(s) proposition(s) Ã  examiner",
                    Priority = "Normal",
                    RequestId = request.Id!,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return alerts.OrderByDescending(a => a.Priority == "Critical" ? 3 : a.Priority == "High" ? 2 : 1)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToList();
    }

    public async Task CheckAndCreateAlertsAsync(string requestId)
    {
        var request = await _requestService.GetRequestByIdAsync(requestId);
        if (request == null) return;

        var alerts = await GetAlertsForPatientAsync(request.PatientId);
        var relevantAlerts = alerts.Where(a => a.RequestId == requestId).ToList();

        foreach (var alert in relevantAlerts)
        {
            // CrÃ©er une notification pour chaque alerte
            await _notificationService.CreateNotificationAsync(
                request.PatientId,
                alert.Type,
                alert.Title,
                alert.Message,
                requestId,
                "Request"
            );
        }
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

public class IntelligentAlert
{
    public string Type { get; set; } = string.Empty; // AidantProche, DemandePrioritaire, NouvellesPropositions
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    public string? RequestId { get; set; }
    public DateTime CreatedAt { get; set; }
}


