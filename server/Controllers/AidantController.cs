using MediAid.Services;
using MediAid.Models;
using MediAid.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize(Policy = "AidantOnly")]
public class AidantController : Controller
{
    private readonly IUserService _userService;
    private readonly IAidantService _aidantService;
    private readonly IRequestService _requestService;
    private readonly IProposalService _proposalService;
    private readonly INotificationService _notificationService;
    private readonly IReviewService _reviewService;
    private readonly IMessageService _messageService;
    private readonly IAidantCommentService _aidantCommentService;

    public AidantController(
        IUserService userService,
        IAidantService aidantService,
        IRequestService requestService,
        IProposalService proposalService,
        INotificationService notificationService,
        IReviewService reviewService,
        IMessageService messageService,
        IAidantCommentService aidantCommentService)
    {
        _userService = userService;
        _aidantService = aidantService;
        _requestService = requestService;
        _proposalService = proposalService;
        _notificationService = notificationService;
        _reviewService = reviewService;
        _messageService = messageService;
        _aidantCommentService = aidantCommentService;
    }

    // Dashboard Aidant
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return RedirectToAction("Index", "Profile");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        
        // Statistiques des demandes
        var allProposals = await _proposalService.GetProposalsByAidantIdAsync(aidant.Id!);
        
        // Calculer les missions en cours et terminées
        int inProgressCount = 0;
        int completedCount = 0;
        foreach (var proposal in allProposals.Where(p => p.Status == "Accepted"))
        {
            var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
            if (request != null)
            {
                if (request.Status == "InProgress")
                    inProgressCount++;
                else if (request.Status == "Completed")
                    completedCount++;
            }
        }

        // Calculer les demandes disponibles - TOUJOURS toutes les demandes ouvertes
        var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
        var filterBuilder = MongoDB.Driver.Builders<MediAid.Models.Request>.Filter;
        var baseFilter = filterBuilder.Eq(r => r.Status, "Open");
        
        // Get available requests for feed
        var availableRequests = await context.Requests
            .Find(baseFilter)
            .SortByDescending(r => r.CreatedAt)
            .Limit(10)
            .ToListAsync();
        
        // Compter TOUTES les demandes ouvertes (avec ou sans localisation, proches ou loin)
        int availableRequestsCount = (int)await context.Requests.CountDocumentsAsync(baseFilter);

        var stats = new AidantDashboardStats
        {
            AvailableRequests = availableRequestsCount,
            AcceptedProposals = allProposals.Count(p => p.Status == "Accepted"),
            InProgressMissions = inProgressCount,
            CompletedMissions = completedCount,
            TotalHours = aidant.TotalHours,
            TotalMissions = aidant.TotalMissions,
            CompletedMissionsCount = aidant.CompletedMissions,
            ReputationScore = aidant.ReputationScore
        };

        // Prochaines missions planifiées
        var upcomingProposals = allProposals
            .Where(p => p.Status == "Accepted")
            .Select(async p => new { Proposal = p, Request = await _requestService.GetRequestByIdAsync(p.RequestId) })
            .ToList();

        var upcomingMissions = new List<object>();
        foreach (var task in upcomingProposals)
        {
            var item = await task;
            if (item.Request != null && (item.Request.Status == "Assigned" || item.Request.Status == "InProgress"))
            {
                upcomingMissions.Add(new
                {
                    RequestId = item.Request.Id,
                    Title = item.Request.Title,
                    RequestedDate = item.Request.RequestedDate,
                    Status = item.Request.Status
                });
            }
        }

        // Liste des autres aidants (pour voir leurs profils)
        var allAidants = await context.Aidants.Find(_ => true).Limit(10).ToListAsync();
        var aidantsList = new List<object>();
        foreach (var a in allAidants.Where(a => a.Id != aidant.Id))
        {
            var aUser = await _userService.GetUserByIdAsync(a.UserId);
            aidantsList.Add(new
            {
                Aidant = a,
                User = aUser
            });
        }

        // Notifications
        var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, unreadOnly: true);
        
        // Messages non lus - calculer le total (réutiliser context déjà défini)
        var unreadMessages = await context.Messages
            .Find(m => m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();
        int totalUnreadMessages = unreadMessages.Count;
        
        // Propositions envoyées avec détails
        var myProposals = new List<object>();
        foreach (var proposal in allProposals.OrderByDescending(p => p.CreatedAt).Take(10))
        {
            var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
            if (request != null)
            {
                var patient = await _userService.GetPatientByUserIdAsync(request.PatientId);
                var patientUser = await _userService.GetUserByIdAsync(request.PatientId);
                
                double? distance = null;
                if (aidant.Location != null && request.Location != null &&
                    aidant.Location.Coordinates.Length >= 2 && request.Location.Coordinates.Length >= 2)
                {
                    distance = CalculateDistance(
                        aidant.Location.Coordinates[1], aidant.Location.Coordinates[0],
                        request.Location.Coordinates[1], request.Location.Coordinates[0]
                    );
                }
                
                myProposals.Add(new
                {
                    Proposal = proposal,
                    Request = request,
                    Patient = patientUser,
                    Distance = distance,
                    Status = proposal.Status
                });
            }
        }
        
        // Missions actives (en cours)
        var activeMissions = new List<object>();
        foreach (var proposal in allProposals.Where(p => p.Status == "Accepted"))
        {
            var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
            if (request != null && (request.Status == "Assigned" || request.Status == "InProgress"))
            {
                var patient = await _userService.GetPatientByUserIdAsync(request.PatientId);
                var patientUser = await _userService.GetUserByIdAsync(request.PatientId);
                
                double? distance = null;
                if (aidant.Location != null && request.Location != null &&
                    aidant.Location.Coordinates.Length >= 2 && request.Location.Coordinates.Length >= 2)
                {
                    distance = CalculateDistance(
                        aidant.Location.Coordinates[1], aidant.Location.Coordinates[0],
                        request.Location.Coordinates[1], request.Location.Coordinates[0]
                    );
                }
                
                activeMissions.Add(new
                {
                    Proposal = proposal,
                    Request = request,
                    Patient = patientUser,
                    Distance = distance,
                    Status = request.Status
                });
            }
        }
        
        // Historique récent (5 dernières missions complétées)
        var recentHistory = new List<object>();
        var completedProposals = allProposals
            .Where(p => p.Status == "Accepted")
            .ToList();
        
        foreach (var proposal in completedProposals)
        {
            var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
            if (request != null && request.Status == "Completed")
            {
                var patient = await _userService.GetPatientByUserIdAsync(request.PatientId);
                var patientUser = await _userService.GetUserByIdAsync(request.PatientId);
                var review = await _reviewService.GetReviewByRequestIdAsync(proposal.RequestId);
                
                recentHistory.Add(new
                {
                    Proposal = proposal,
                    Request = request,
                    Patient = patientUser,
                    Review = review,
                    CompletedAt = request.CompletedAt ?? request.UpdatedAt
                });
            }
        }
        recentHistory = recentHistory.OrderByDescending(h => ((DateTime)((dynamic)h).CompletedAt)).Take(5).ToList();
        
        // Calculer les vies touchées (patients uniques aidés)
        var uniquePatientsHelped = completedProposals
            .Select(async p => await _requestService.GetRequestByIdAsync(p.RequestId))
            .Select(t => t.Result)
            .Where(r => r != null && r.Status == "Completed")
            .Select(r => r!.PatientId)
            .Distinct()
            .Count();
        
        // Reviews pour réputation
        var allReviews = await _reviewService.GetReviewsByAidantIdAsync(aidant.Id!);
        
        ViewBag.Stats = stats;
        ViewBag.Aidant = aidant;
        ViewBag.User = user;
        ViewBag.UpcomingMissions = upcomingMissions;
        ViewBag.UnreadNotifications = notifications.Count;
        ViewBag.RecentNotifications = notifications.Take(5).ToList();
        ViewBag.AvailableRequests = availableRequests;
        ViewBag.MyProposals = myProposals;
        ViewBag.ActiveMissions = activeMissions;
        ViewBag.RecentHistory = recentHistory;
        ViewBag.TotalUnreadMessages = totalUnreadMessages;
        ViewBag.UniquePatientsHelped = uniquePatientsHelped;
        ViewBag.AllReviews = allReviews;
        ViewBag.AidantsList = aidantsList;

        return View();
    }

    // Helper method to calculate distance between two coordinates
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
        return degrees * (Math.PI / 180);
    }

    // Historique des missions
    public async Task<IActionResult> History(string? category, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return RedirectToAction("Index", "Profile");
        }

        var allProposals = await _proposalService.GetProposalsByAidantIdAsync(aidant.Id!);
        
        // Filtrer par statut
        if (!string.IsNullOrEmpty(status))
        {
            allProposals = allProposals.Where(p => p.Status == status).ToList();
        }

        // Enrichir avec les demandes
        var missions = new List<MissionHistoryItem>();
        foreach (var proposal in allProposals)
        {
            var request = await _requestService.GetRequestByIdAsync(proposal.RequestId);
            if (request != null)
            {
                // Filtrer par catégorie
                if (!string.IsNullOrEmpty(category) && request.Category != category)
                    continue;

                // Filtrer par date
                if (fromDate.HasValue && request.CreatedAt < fromDate.Value)
                    continue;
                if (toDate.HasValue && request.CreatedAt > toDate.Value)
                    continue;

                var review = await _reviewService.GetReviewByRequestIdAsync(proposal.RequestId);
                
                missions.Add(new MissionHistoryItem
                {
                    Proposal = proposal,
                    Request = request,
                    Review = review
                });
            }
        }

        ViewBag.Missions = missions.OrderByDescending(m => m.Request.CreatedAt).ToList();
        ViewBag.Aidant = aidant;
        
        return View();
    }

    // Voir le profil d'un aidant
    [HttpGet]
    public async Task<IActionResult> Profile(string id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var currentAidant = await _userService.GetAidantByUserIdAsync(currentUserId);
        
        if (currentAidant == null)
        {
            return RedirectToAction("Dashboard");
        }

        // Récupérer l'aidant dont on veut voir le profil
        Aidant? targetAidant = null;
        User? targetUser = null;

        // Récupérer le contexte MongoDB une seule fois
        var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
        
        if (!string.IsNullOrEmpty(id))
        {
            // Si un ID est fourni, chercher par ID d'aidant
            targetAidant = await context.Aidants.Find(a => a.Id == id).FirstOrDefaultAsync();
            if (targetAidant != null)
            {
                targetUser = await _userService.GetUserByIdAsync(targetAidant.UserId);
            }
        }
        else
        {
            // Sinon, afficher le profil de l'aidant connecté
            targetAidant = currentAidant;
            targetUser = await _userService.GetUserByIdAsync(currentUserId);
        }

        if (targetAidant == null || targetUser == null)
        {
            return NotFound();
        }

        // Récupérer les commentaires sur cet aidant
        var comments = await _aidantCommentService.GetCommentsByAidantIdAsync(targetAidant.Id!);
        
        // Enrichir les commentaires avec les informations des auteurs
        var commentsWithAuthors = new List<object>();
        foreach (var comment in comments)
        {
            var authorAidant = await context.Aidants.Find(a => a.Id == comment.AuthorAidantId).FirstOrDefaultAsync();
            var authorUser = authorAidant != null ? await _userService.GetUserByIdAsync(authorAidant.UserId) : null;
            
            commentsWithAuthors.Add(new
            {
                Comment = comment,
                AuthorAidant = authorAidant,
                AuthorUser = authorUser
            });
        }

        // Vérifier si l'aidant connecté a déjà laissé un commentaire
        var existingComment = await _aidantCommentService.GetCommentByAuthorAndTargetAsync(
            currentAidant.Id!, 
            targetAidant.Id!
        );

        // Récupérer les reviews des patients pour cet aidant
        var reviews = await _reviewService.GetReviewsByAidantIdAsync(targetAidant.Id!);
        var reviewsWithPatients = new List<object>();
        foreach (var review in reviews.Take(10))
        {
            var request = await _requestService.GetRequestByIdAsync(review.RequestId);
            if (request != null)
            {
                var patientUser = await _userService.GetUserByIdAsync(request.PatientId);
                reviewsWithPatients.Add(new
                {
                    Review = review,
                    Request = request,
                    Patient = patientUser
                });
            }
        }

        ViewBag.TargetAidant = targetAidant;
        ViewBag.TargetUser = targetUser;
        ViewBag.CurrentAidant = currentAidant;
        ViewBag.Comments = commentsWithAuthors;
        ViewBag.ExistingComment = existingComment;
        ViewBag.Reviews = reviewsWithPatients;
        ViewBag.IsOwnProfile = currentAidant.Id == targetAidant.Id;

        return View();
    }

    // Ajouter ou modifier un commentaire sur un aidant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(string targetAidantId, string content, int? rating)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var currentAidant = await _userService.GetAidantByUserIdAsync(currentUserId);
        
        if (currentAidant == null || string.IsNullOrEmpty(currentAidant.Id))
        {
            return Unauthorized();
        }

        if (currentAidant.Id == targetAidantId)
        {
            TempData["ErrorMessage"] = "Vous ne pouvez pas commenter votre propre profil.";
            return RedirectToAction("Profile", new { id = targetAidantId });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Le commentaire ne peut pas être vide.";
            return RedirectToAction("Profile", new { id = targetAidantId });
        }

        // Vérifier si un commentaire existe déjà
        var existingComment = await _aidantCommentService.GetCommentByAuthorAndTargetAsync(
            currentAidant.Id, 
            targetAidantId
        );

        if (existingComment != null)
        {
            // Mettre à jour le commentaire existant
            existingComment.Content = content;
            existingComment.Rating = rating;
            existingComment.UpdatedAt = DateTime.UtcNow;
            await _aidantCommentService.UpdateCommentAsync(existingComment);
            TempData["SuccessMessage"] = "Commentaire mis à jour avec succès.";
        }
        else
        {
            // Créer un nouveau commentaire
            var comment = new MediAid.Models.AidantComment
            {
                TargetAidantId = targetAidantId,
                AuthorAidantId = currentAidant.Id,
                Content = content,
                Rating = rating,
                IsPublic = true
            };
            await _aidantCommentService.CreateCommentAsync(comment);
            TempData["SuccessMessage"] = "Commentaire ajouté avec succès.";
        }

        return RedirectToAction("Profile", new { id = targetAidantId });
    }

    // Supprimer un commentaire
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(string commentId, string targetAidantId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var currentAidant = await _userService.GetAidantByUserIdAsync(currentUserId);
        
        if (currentAidant == null || string.IsNullOrEmpty(currentAidant.Id))
        {
            return Unauthorized();
        }

        var comment = await _aidantCommentService.GetCommentByIdAsync(commentId);
        if (comment == null)
        {
            return NotFound();
        }

        // Vérifier que l'aidant connecté est l'auteur du commentaire
        if (comment.AuthorAidantId != currentAidant.Id)
        {
            return Forbid();
        }

        await _aidantCommentService.DeleteCommentAsync(commentId);
        TempData["SuccessMessage"] = "Commentaire supprimé avec succès.";

        return RedirectToAction("Profile", new { id = targetAidantId });
    }
}

public class AidantDashboardStats
{
    public int AvailableRequests { get; set; }
    public int AcceptedProposals { get; set; }
    public int InProgressMissions { get; set; }
    public int CompletedMissions { get; set; }
    public double TotalHours { get; set; }
    public int TotalMissions { get; set; }
    public int CompletedMissionsCount { get; set; }
    public double ReputationScore { get; set; }
}

public class MissionHistoryItem
{
    public Proposal Proposal { get; set; } = null!;
    public Request Request { get; set; } = null!;
    public Review? Review { get; set; }
}

