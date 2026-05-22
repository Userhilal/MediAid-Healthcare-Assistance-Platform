using MediAid.Services;
using MediAid.Data;
using MediAid.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Security.Claims;
using System.Linq;

namespace MediAid.Controllers;

[Authorize(Policy = "AidantOnly")]
public class ProposalController : Controller
{
    private readonly IRequestService _requestService;
    private readonly IProposalService _proposalService;
    private readonly IUserService _userService;
    private readonly IAidantService _aidantService;

    public ProposalController(IRequestService requestService, IProposalService proposalService,
        IUserService userService, IAidantService aidantService)
    {
        _requestService = requestService;
        _proposalService = proposalService;
        _userService = userService;
        _aidantService = aidantService;
    }

    public async Task<IActionResult> Index(string? category, string? urgency)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        
        List<MediAid.Models.Request> requests;
        string? message = null;
        
        if (aidant == null)
        {
            message = "Votre profil aidant n'est pas encore configuré. Veuillez compléter votre profil.";
            requests = new List<MediAid.Models.Request>();
        }
        else if (aidant.Location == null)
        {
            // Afficher toutes les demandes ouvertes si pas de localisation (mode fallback)
            message = "Attention Localisation non configurée. Toutes les demandes sont affichées. Configurez votre localisation pour voir uniquement les demandes à proximité.";
            
            // Récupérer TOUTES les demandes ouvertes (avec ou sans localisation)
            var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
            var filterBuilder = MongoDB.Driver.Builders<MediAid.Models.Request>.Filter;
            var baseFilter = filterBuilder.Eq(r => r.Status, "Open");
            
            if (!string.IsNullOrEmpty(category))
            {
                baseFilter = filterBuilder.And(baseFilter, filterBuilder.Eq(r => r.Category, category));
            }
            if (!string.IsNullOrEmpty(urgency))
            {
                baseFilter = filterBuilder.And(baseFilter, filterBuilder.Eq(r => r.Urgency, urgency));
            }
            
            requests = await context.Requests.Find(baseFilter)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
        else
        {
            var latitude = aidant.Location.Coordinates[1];
            var longitude = aidant.Location.Coordinates[0];
            var radius = aidant.InterventionRadius;

            // Récupérer toutes les demandes ouvertes (avec et sans localisation)
            var context = HttpContext.RequestServices.GetRequiredService<MongoDbContext>();
            var filterBuilder = Builders<MediAid.Models.Request>.Filter;
            var baseFilter = filterBuilder.Eq(r => r.Status, "Open");
            
            if (!string.IsNullOrEmpty(category))
            {
                baseFilter = filterBuilder.And(baseFilter, filterBuilder.Eq(r => r.Category, category));
            }
            if (!string.IsNullOrEmpty(urgency))
            {
                baseFilter = filterBuilder.And(baseFilter, filterBuilder.Eq(r => r.Urgency, urgency));
            }
            
            var allOpenRequests = await context.Requests.Find(baseFilter)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync();

            // TOUJOURS inclure toutes les demandes ouvertes (avec ou sans localisation, proches ou loin)
            requests = new List<MediAid.Models.Request>();
            var requestDistances = new Dictionary<string, double>();
            
            foreach (var request in allOpenRequests)
            {
                // Toujours inclure les demandes sans localisation
                if (request.Location?.Coordinates == null || request.Location.Coordinates.Length < 2)
                {
                    requests.Add(request);
                    continue;
                }

                // Pour les demandes avec localisation, toujours les inclure (même si loin)
                // On calcule quand même la distance pour l'affichage
                var requestLat = request.Location.Coordinates[1];
                var requestLon = request.Location.Coordinates[0];
                var distance = CalculateDistance(latitude, longitude, requestLat, requestLon);
                
                // Toujours inclure, même si hors du rayon
                requests.Add(request);
                if (request.Id != null)
                {
                    requestDistances[request.Id] = distance;
                }
            }
            
            ViewBag.RequestDistances = requestDistances;
            
            if (!requests.Any())
            {
                message = "Aucune demande ouverte disponible pour le moment.";
            }
            else
            {
                ViewBag.RequestsCount = requests.Count;
                var withoutLocation = requests.Count(r => r.Location == null);
                var nearbyCount = 0;
                var farCount = 0;
                
                foreach (var request in requests.Where(r => r.Location != null && r.Location.Coordinates != null && r.Location.Coordinates.Length >= 2))
                {
                    var requestLat = request.Location!.Coordinates[1];
                    var requestLon = request.Location.Coordinates[0];
                    var distance = CalculateDistance(latitude, longitude, requestLat, requestLon);
                    if (distance <= radius)
                        nearbyCount++;
                    else
                        farCount++;
                }
                
                var parts = new List<string>();
                if (nearbyCount > 0) parts.Add($"{nearbyCount} à proximité");
                if (farCount > 0) parts.Add($"{farCount} plus éloignées");
                if (withoutLocation > 0) parts.Add($"{withoutLocation} sans localisation");
                
                message = $"{requests.Count} demande(s) disponible(s) ({string.Join(", ", parts)}).";
                ViewBag.NearbyCount = nearbyCount;
            }
        }
        
        ViewBag.Message = message;
        ViewBag.Aidant = aidant;
        ViewBag.User = await _userService.GetUserByIdAsync(userId);
        return View(requests);
    }

    // Get request details partial for right pane
    [HttpGet]
    public async Task<IActionResult> DetailsPartial(string id)
    {
        var request = await _requestService.GetRequestByIdAsync(id);
        if (request == null)
        {
            return PartialView("_RequestDetailsPartial", null);
        }

        // Get patient info
        var patient = await _userService.GetPatientByUserIdAsync(request.PatientId);
        
        // Calculate distance if aidant has location
        double? distance = null;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        
        if (aidant?.Location != null && request.Location != null && request.Location.Coordinates?.Length >= 2)
        {
            var aidantLat = aidant.Location.Coordinates[1];
            var aidantLon = aidant.Location.Coordinates[0];
            var requestLat = request.Location.Coordinates[1];
            var requestLon = request.Location.Coordinates[0];
            distance = CalculateDistance(aidantLat, aidantLon, requestLat, requestLon);
        }

        ViewBag.Request = request;
        ViewBag.Patient = patient;
        ViewBag.Distance = distance;
        ViewBag.Aidant = aidant;
        
        return PartialView("_RequestDetailsPartial", request);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string requestId)
    {
        var request = await _requestService.GetRequestByIdAsync(requestId);
        if (request == null)
        {
            return NotFound();
        }

        ViewBag.Request = request;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string requestId, string? message, DateTime? estimatedArrivalTime)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        if (aidant == null)
        {
            return NotFound();
        }

        var proposal = await _proposalService.CreateProposalAsync(requestId, aidant.Id!, message, estimatedArrivalTime);
        if (proposal == null)
        {
            TempData["ErrorMessage"] = "Impossible de créer cette proposition.";
            return RedirectToAction("Index");
        }

        TempData["SuccessMessage"] = "Proposition créée avec succès !";
        return RedirectToAction("MyProposals");
    }

    public async Task<IActionResult> MyProposals()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        if (aidant == null)
        {
            return NotFound();
        }

        var user = await _userService.GetUserByIdAsync(userId);
        var proposals = await _proposalService.GetProposalsByAidantIdAsync(aidant.Id!);
        
        // Enrichir les propositions avec les demandes
        var enrichedProposals = new List<object>();
        foreach (var proposal in proposals)
        {
            var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
            if (request != null)
            {
                var patient = await _userService.GetPatientByUserIdAsync(request.PatientId);
                var patientUser = await _userService.GetUserByIdAsync(request.PatientId);
                
                // Calculer la distance si possible
                double? distance = null;
                if (aidant.Location != null && request.Location != null && 
                    aidant.Location.Coordinates.Length >= 2 && request.Location.Coordinates.Length >= 2)
                {
                    distance = CalculateDistance(
                        aidant.Location.Coordinates[1], aidant.Location.Coordinates[0],
                        request.Location.Coordinates[1], request.Location.Coordinates[0]
                    );
                }
                
                enrichedProposals.Add(new
                {
                    Proposal = proposal,
                    Request = request,
                    Patient = patient,
                    PatientUser = patientUser,
                    Distance = distance,
                    Status = proposal.Status,
                    IsActive = proposal.Status == "Accepted" && (request.Status == "Assigned" || request.Status == "InProgress")
                });
            }
        }
        
        // Prochaine mission active
        var nextActiveMission = enrichedProposals
            .Where(p => ((dynamic)p).IsActive == true)
            .OrderBy(p => ((dynamic)p).Request.RequestedDate ?? DateTime.MaxValue)
            .FirstOrDefault();
        
        // Missions acceptées
        var acceptedMissions = enrichedProposals
            .Where(p => ((dynamic)p).Proposal.Status == "Accepted")
            .OrderByDescending(p => ((dynamic)p).Proposal.CreatedAt)
            .ToList();
        
        // Statistiques
        var stats = new
        {
            TotalProposals = proposals.Count,
            AcceptedCount = proposals.Count(p => p.Status == "Accepted"),
            InProgressCount = proposals.Count(p => p.Status == "Accepted" && 
                enrichedProposals.Any(ep => ((dynamic)ep).Proposal.Id == p.Id && 
                (((dynamic)ep).Request.Status == "Assigned" || ((dynamic)ep).Request.Status == "InProgress"))),
            CompletedCount = proposals.Count(p => p.Status == "Accepted" && 
                enrichedProposals.Any(ep => ((dynamic)ep).Proposal.Id == p.Id && 
                ((dynamic)ep).Request.Status == "Completed")),
            PendingCount = proposals.Count(p => p.Status == "Pending")
        };
        
        // Messages non lus (via chat)
        var context = HttpContext.RequestServices.GetRequiredService<MongoDbContext>();
        var unreadMessages = await context.Messages
            .Find(m => m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();
        
        // Calculer les heures réelles par semaine (7 dernières semaines)
        var weeklyHours = new List<double>();
        var now = DateTime.UtcNow;
        for (int i = 6; i >= 0; i--)
        {
            var weekStart = now.AddDays(-(i * 7)).Date;
            var weekEnd = weekStart.AddDays(7);
            
            // Récupérer les missions complétées dans cette semaine
            var weekMissions = enrichedProposals
                .Where(ep => 
                {
                    var req = ((dynamic)ep).Request as Request;
                    if (req == null || req.Status != "Completed") return false;
                    
                    // Utiliser CompletedAt si disponible, sinon UpdatedAt
                    var completionDate = req.CompletedAt ?? req.UpdatedAt;
                    return completionDate >= weekStart && completionDate < weekEnd;
                })
                .ToList();
            
            // Calculer les heures pour cette semaine (basé sur la durée réelle)
            double weekHours = 0.0;
            foreach (var mission in weekMissions)
            {
                var req = ((dynamic)mission).Request as Request;
                if (req != null)
                {
                    // Utiliser CompletedAt si disponible
                    var endDate = req.CompletedAt.HasValue ? req.CompletedAt.Value : req.UpdatedAt;
                    
                    if (req.RequestedDate.HasValue)
                    {
                        var duration = endDate - req.RequestedDate.Value;
                        if (duration.TotalHours > 0 && duration.TotalHours < 24) // Validation raisonnable
                        {
                            weekHours += duration.TotalHours;
                        }
                        else
                        {
                            // Estimation par défaut : 1 heure par mission si durée invalide
                            weekHours += 1.0;
                        }
                    }
                    else
                    {
                        // Estimation par défaut : 1 heure par mission
                        weekHours += 1.0;
                    }
                }
            }
            weeklyHours.Add(Math.Round(weekHours, 1));
        }
        
        // Récupérer les reviews réelles pour calculer la réputation
        var reviewService = HttpContext.RequestServices.GetRequiredService<IReviewService>();
        var reviews = await reviewService.GetReviewsByAidantIdAsync(aidant.Id!);
        var realReputationScore = aidant.ReputationScore; // Déjà calculé dans ReviewService
        
        // Calculer le nombre réel de vies touchées (patients uniques aidés)
        var uniquePatients = enrichedProposals
            .Where(ep => ((dynamic)ep).Proposal.Status == "Accepted")
            .Select(ep => ((dynamic)ep).Request.PatientId)
            .Distinct()
            .Count();
        
        // Récupérer les badges réels
        var realBadges = aidant.Badges ?? new List<string>();
        
        // Calculer les heures totales réelles depuis les missions complétées
        var completedMissions = enrichedProposals
            .Where(ep => 
            {
                var req = ((dynamic)ep).Request as Request;
                return req != null && req.Status == "Completed";
            })
            .ToList();
        
        double realTotalHours = 0.0;
        foreach (var mission in completedMissions)
        {
            var req = ((dynamic)mission).Request as Request;
            if (req != null)
            {
                // Utiliser CompletedAt si disponible
                var endDate = req.CompletedAt.HasValue ? req.CompletedAt.Value : req.UpdatedAt;
                
                if (req.RequestedDate.HasValue)
                {
                    var duration = endDate - req.RequestedDate.Value;
                    if (duration.TotalHours > 0 && duration.TotalHours < 24) // Validation raisonnable
                    {
                        realTotalHours += duration.TotalHours;
                    }
                    else
                    {
                        realTotalHours += 1.0; // Estimation par défaut si durée invalide
                    }
                }
                else
                {
                    realTotalHours += 1.0; // Estimation par défaut
                }
            }
        }
        
        // Si TotalHours dans la DB est plus grand, utiliser celui-ci (peut être mis à jour manuellement)
        if (aidant.TotalHours > realTotalHours)
        {
            realTotalHours = aidant.TotalHours;
        }
        
        ViewBag.EnrichedProposals = enrichedProposals;
        ViewBag.NextActiveMission = nextActiveMission;
        ViewBag.AcceptedMissions = acceptedMissions;
        ViewBag.Stats = stats;
        ViewBag.Aidant = aidant;
        ViewBag.User = user;
        ViewBag.UnreadMessagesCount = unreadMessages.Count;
        ViewBag.Proposals = proposals;
        ViewBag.WeeklyHours = weeklyHours; // Données réelles pour le graphique
        ViewBag.RealReputationScore = realReputationScore;
        ViewBag.UniquePatientsCount = uniquePatients; // Vies touchées réelles
        ViewBag.RealBadges = realBadges;
        ViewBag.RealTotalHours = realTotalHours;
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string proposalId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        if (aidant == null)
        {
            return NotFound();
        }

        var result = await _proposalService.CancelProposalAsync(proposalId, aidant.Id!);
        if (!result)
        {
            TempData["ErrorMessage"] = "Impossible d'annuler cette proposition.";
        }
        else
        {
            TempData["SuccessMessage"] = "Proposition annulée avec succès.";
        }

        return RedirectToAction("MyProposals");
    }
    
    // Helper method pour calculer la distance
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequestStatus(string requestId, string newStatus)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);

        if (aidant == null || string.IsNullOrWhiteSpace(aidant.Id))
        {
            return Json(new { success = false, message = "Aidant non trouvé." });
        }

        var request = await _requestService.GetRequestByIdAsync(requestId);

        if (request == null)
        {
            return Json(new { success = false, message = "Demande non trouvée." });
        }

        if (request.AssignedAidantId != aidant.Id)
        {
            return Json(new { success = false, message = "Vous n'êtes pas assigné à cette demande." });
        }

        if (newStatus == "Completed")
        {
            return Json(new
            {
                success = false,
                message = "La mission ne peut pas être terminée directement. Ajoutez une preuve ou demandez une vérification patient."
            });
        }

        if (request.Status != "Assigned" || newStatus != "InProgress")
        {
            return Json(new { success = false, message = "Transition de statut invalide." });
        }

        request.Status = "InProgress";
        request.UpdatedAt = DateTime.UtcNow;

        var result = await _requestService.UpdateRequestAsync(request);

        if (!result)
        {
            return Json(new { success = false, message = "Erreur lors de la mise à jour." });
        }

        var context = HttpContext.RequestServices.GetRequiredService<MongoDbContext>();
        var aidantToUpdate = await context.Aidants.Find(a => a.Id == aidant.Id).FirstOrDefaultAsync();

        if (aidantToUpdate != null)
        {
            aidantToUpdate.AvailabilityStatus = "Busy";
            aidantToUpdate.UpdatedAt = DateTime.UtcNow;
            await context.Aidants.ReplaceOneAsync(a => a.Id == aidantToUpdate.Id, aidantToUpdate);
        }

        var notificationService = HttpContext.RequestServices.GetRequiredService<INotificationService>();

        await notificationService.CreateNotificationAsync(
            request.PatientId,
            "MissionStarted",
            "Mission démarrée",
            $"L'aidant a commencé la mission « {request.Title} ».",
            request.Id,
            "Request"
        );

        return Json(new { success = true, message = "Mission démarrée avec succès." });
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Rayon de la Terre en kilomètres
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





