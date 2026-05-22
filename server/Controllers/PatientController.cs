using MediAid.DTOs;
using MediAid.Models;
using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MediAid.Controllers;

[Authorize(Policy = "PatientOnly")]
public class PatientController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IRequestService _requestService;
    private readonly IProposalService _proposalService;
    private readonly IUserService _userService;
    private readonly IIntelligentAlertService _alertService;

    public PatientController(IPatientService patientService, IRequestService requestService,
        IProposalService proposalService, IUserService userService, IIntelligentAlertService alertService)
    {
        _patientService = patientService;
        _requestService = requestService;
        _proposalService = proposalService;
        _userService = userService;
        _alertService = alertService;
    }

    // Tableau de bord Patient
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var stats = await _patientService.GetDashboardStatsAsync(userId);
        var alerts = await _alertService.GetAlertsForPatientAsync(userId);
        var user = await _userService.GetUserByIdAsync(userId);
        var patient = await _patientService.GetPatientByUserIdAsync(userId);
        
        // Get all requests for various purposes
        var allRequests = await _patientService.GetPatientRequestHistoryAsync(userId);
        var latestRequest = allRequests.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
        
        // Get active requests (all statuses except Completed and Cancelled)
        var activeRequests = allRequests
            .Where(r => r.Status != "Completed" && r.Status != "Cancelled")
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        
        // Get scheduled requests (with RequestedDate in future)
        var scheduledRequests = allRequests
            .Where(r => r.RequestedDate.HasValue && r.RequestedDate.Value > DateTime.UtcNow)
            .OrderBy(r => r.RequestedDate)
            .ToList();
        
        // Get requests for today, tomorrow, this week
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var weekEnd = today.AddDays(7);
        
        var requestsToday = scheduledRequests
            .Where(r => r.RequestedDate.HasValue && r.RequestedDate.Value.Date == today)
            .ToList();
        
        var requestsTomorrow = scheduledRequests
            .Where(r => r.RequestedDate.HasValue && r.RequestedDate.Value.Date == tomorrow)
            .ToList();
        
        var requestsThisWeek = scheduledRequests
            .Where(r => r.RequestedDate.HasValue && r.RequestedDate.Value.Date >= today && r.RequestedDate.Value.Date < weekEnd)
            .ToList();
        
        // Get messages/conversations
        var messageService = HttpContext.RequestServices.GetRequiredService<IMessageService>();
        var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
        
        // Get unread messages count
        var unreadMessages = await context.Messages
            .Find(m => m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();
        int totalUnreadMessages = unreadMessages.Count;
        
        // Get conversations (requests with assigned aidant)
        var conversations = new List<object>();
        foreach (var req in activeRequests.Where(r => !string.IsNullOrEmpty(r.AssignedAidantId)))
        {
            var aidant = await context.Aidants.Find(a => a.Id == req.AssignedAidantId).FirstOrDefaultAsync();
            if (aidant != null)
            {
                var aidantUser = await _userService.GetUserByIdAsync(aidant.UserId);
                var messages = await messageService.GetMessagesByRequestIdAsync(req.Id!);
                var lastMessage = messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                var unreadCount = await messageService.GetUnreadMessageCountAsync(userId, req.Id!);
                
                conversations.Add(new
                {
                    Request = req,
                    Aidant = aidantUser,
                    LastMessage = lastMessage,
                    UnreadCount = unreadCount
                });
            }
        }
        
        // Get notifications
        var notificationService = HttpContext.RequestServices.GetRequiredService<INotificationService>();
        var allNotifications = await notificationService.GetNotificationsByUserIdAsync(userId, unreadOnly: false);
        
        // Get review service to check pending feedback
        var reviewService = HttpContext.RequestServices.GetRequiredService<IReviewService>();
        var completedRequestsWithoutReview = allRequests
            .Where(r => r.Status == "Completed" && !string.IsNullOrEmpty(r.AssignedAidantId))
            .ToList();
        
        var pendingFeedbackRequests = new List<MediAid.Models.Request>();
        foreach (var req in completedRequestsWithoutReview)
        {
            var review = await reviewService.GetReviewByRequestIdAsync(req.Id!);
            if (review == null)
            {
                pendingFeedbackRequests.Add(req);
            }
        }
        
        // Get request history with reviews and aidant info
        var requestHistory = new List<object>();
        foreach (var req in allRequests.OrderByDescending(r => r.CreatedAt).Take(10))
        {
            var review = await reviewService.GetReviewByRequestIdAsync(req.Id!);
            User? aidantUser = null;
            if (!string.IsNullOrEmpty(req.AssignedAidantId))
            {
                var aidant = await context.Aidants.Find(a => a.Id == req.AssignedAidantId).FirstOrDefaultAsync();
                if (aidant != null)
                {
                    aidantUser = await _userService.GetUserByIdAsync(aidant.UserId);
                }
            }
            
            requestHistory.Add(new
            {
                Request = req,
                Review = review,
                Aidant = aidantUser
            });
        }
        
        // Get assigned aidant user info if available
        User? assignedAidantUser = null;
        List<Review> aidantRecentReviews = new List<Review>();
        if (stats.AssignedAidant != null && !string.IsNullOrEmpty(stats.AssignedAidant.UserId) && !string.IsNullOrEmpty(stats.AssignedAidant.Id))
        {
            assignedAidantUser = await _userService.GetUserByIdAsync(stats.AssignedAidant.UserId);
            
            // Get recent reviews for this aidant (last 3)
            aidantRecentReviews = await reviewService.GetReviewsByAidantIdAsync(stats.AssignedAidant.Id!);
            aidantRecentReviews = aidantRecentReviews.Take(3).ToList();
            
            // Find a completed request without review for this aidant
            var completedRequestForReview = allRequests
                .Where(r => r.Status == "Completed" && r.AssignedAidantId == stats.AssignedAidant.Id)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();
            
            if (completedRequestForReview != null)
            {
                var review = await reviewService.GetReviewByRequestIdAsync(completedRequestForReview.Id!);
                if (review == null)
                {
                    ViewBag.CompletedRequestForReview = completedRequestForReview;
                }
            }
        }
        
        ViewBag.Alerts = alerts;
        ViewBag.User = user;
        ViewBag.Patient = patient;
        ViewBag.LatestRequest = latestRequest;
        ViewBag.AllNotifications = allNotifications;
        ViewBag.PendingFeedbackRequests = pendingFeedbackRequests;
        ViewBag.RequestHistory = requestHistory;
        ViewBag.CurrentActiveRequest = stats.CurrentActiveRequest;
        ViewBag.AssignedAidant = stats.AssignedAidant;
        ViewBag.AssignedAidantUser = assignedAidantUser;
        ViewBag.ActiveRequests = activeRequests;
        ViewBag.ScheduledRequests = scheduledRequests;
        ViewBag.RequestsToday = requestsToday;
        ViewBag.RequestsTomorrow = requestsTomorrow;
        ViewBag.RequestsThisWeek = requestsThisWeek;
        ViewBag.Conversations = conversations;
        ViewBag.TotalUnreadMessages = totalUnreadMessages;
        ViewBag.AidantRecentReviews = aidantRecentReviews;
        
        return View(stats);
    }

    // Contact Emergency Contact
    [HttpPost]
    public async Task<IActionResult> ContactEmergency(string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _patientService.GetPatientByUserIdAsync(userId);
        
        if (patient?.EmergencyContact == null)
        {
            return Json(new { success = false, message = "Aucun contact d'urgence configurÃ©" });
        }

        var request = await _requestService.GetRequestByIdAsync(requestId);
        if (request == null || request.PatientId != userId)
        {
            return Json(new { success = false, message = "Demande non trouvÃ©e" });
        }

        // Get aidant info if assigned
        string aidantInfo = "Aucun aidant assignÃ©";
        if (!string.IsNullOrEmpty(request.AssignedAidantId))
        {
            var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
            var aidant = await context.Aidants.Find(a => a.Id == request.AssignedAidantId).FirstOrDefaultAsync();
            if (aidant != null)
            {
                var aidantUser = await _userService.GetUserByIdAsync(aidant.UserId);
                aidantInfo = aidantUser != null ? $"{aidantUser.FirstName} {aidantUser.LastName}" : "Aidant assignÃ©";
            }
        }

        // Create notification for emergency contact (in a real app, you'd send SMS/Email)
        // For now, we'll just return success
        // In production, integrate with SMS/Email service to notify the emergency contact
        
        var statusMessage = request.Status switch
        {
            "Assigned" => "Une aide a Ã©tÃ© assignÃ©e",
            "InProgress" => "L'aide est en cours",
            "Completed" => "L'aide a Ã©tÃ© complÃ©tÃ©e",
            _ => "Statut: " + request.Status
        };

        return Json(new { 
            success = true, 
            message = "Contact d'urgence notifiÃ©",
            contactName = patient.EmergencyContact.Name,
            missionStatus = statusMessage,
            aidantInfo = aidantInfo
        });
    }

    // Profil Patient
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _patientService.GetPatientByUserIdAsync(userId);
        var user = await _userService.GetUserByIdAsync(userId);

        if (patient == null)
        {
            // Create empty patient profile
            patient = new MediAid.Models.Patient
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        var dto = new PatientProfileDto
        {
            FirstName = user?.FirstName,
            LastName = user?.LastName,
            PhoneNumber = user?.PhoneNumber,
            ProfilePhoto = patient.ProfilePhoto,
            DateOfBirth = patient.DateOfBirth,
            Address = patient.Address,
            City = patient.City,
            PostalCode = patient.PostalCode,
            LocationBlurRadius = patient.LocationBlurRadius,
            ContactPreference = patient.ContactPreference,
            AnonymousMode = patient.AnonymousMode,
            MedicalConditions = patient.MedicalConditions
        };

        if (patient.Location != null && patient.Location.Coordinates.Length >= 2)
        {
            dto.Latitude = patient.Location.Coordinates[1];
            dto.Longitude = patient.Location.Coordinates[0];
        }

        if (patient.EmergencyContact != null)
        {
            dto.EmergencyContact = new EmergencyContactDto
            {
                Name = patient.EmergencyContact.Name,
                PhoneNumber = patient.EmergencyContact.PhoneNumber,
                Relationship = patient.EmergencyContact.Relationship
            };
        }

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(PatientProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await _patientService.UpdatePatientProfileAsync(userId, dto);

        if (result)
        {
            TempData["SuccessMessage"] = "Profil mis Ã  jour avec succÃ¨s.";
        }
        else
        {
            TempData["ErrorMessage"] = "Erreur lors de la mise Ã  jour du profil.";
        }

        return RedirectToAction("Profile");
    }

    [HttpPost]
    public async Task<IActionResult> AddTrustedContact(string name, string relationship, string phoneNumber)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _patientService.GetPatientByUserIdAsync(userId);
        
        if (patient == null)
        {
            return Json(new { success = false, message = "Profil patient introuvable" });
        }

        if (patient.TrustedContacts == null)
        {
            patient.TrustedContacts = new List<EmergencyContact>();
        }

        var newContact = new EmergencyContact
        {
            Name = name,
            Relationship = relationship,
            PhoneNumber = phoneNumber
        };

        patient.TrustedContacts.Add(newContact);
        patient.UpdatedAt = DateTime.UtcNow;

        var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
        var result = await context.Patients.ReplaceOneAsync(p => p.Id == patient.Id, patient);

        if (result.ModifiedCount > 0)
        {
            return Json(new { success = true, message = "Contact ajoutÃ© avec succÃ¨s" });
        }

        return Json(new { success = false, message = "Erreur lors de l'ajout du contact" });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveTrustedContact(string name, string phoneNumber)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _patientService.GetPatientByUserIdAsync(userId);
        
        if (patient == null || patient.TrustedContacts == null)
        {
            return Json(new { success = false, message = "Contact introuvable" });
        }

        var contactToRemove = patient.TrustedContacts.FirstOrDefault(c => c.Name == name && c.PhoneNumber == phoneNumber);
        if (contactToRemove != null)
        {
            patient.TrustedContacts.Remove(contactToRemove);
            patient.UpdatedAt = DateTime.UtcNow;

            var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
            var result = await context.Patients.ReplaceOneAsync(p => p.Id == patient.Id, patient);

            if (result.ModifiedCount > 0)
            {
                return Json(new { success = true, message = "Contact supprimÃ© avec succÃ¨s" });
            }
        }

        return Json(new { success = false, message = "Erreur lors de la suppression du contact" });
    }

    // Historique des demandes
    public async Task<IActionResult> History()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var requests = await _patientService.GetPatientRequestHistoryAsync(userId);
        
        // RÃ©cupÃ©rer les reviews existantes pour les demandes complÃ©tÃ©es
        var reviewService = HttpContext.RequestServices.GetRequiredService<IReviewService>();
        var reviewsByRequestId = new Dictionary<string, bool>();
        foreach (var request in requests.Where(r => r.Status == "Completed"))
        {
            var review = await reviewService.GetReviewByRequestIdAsync(request.Id!);
            reviewsByRequestId[request.Id!] = review != null;
        }
        ViewBag.ReviewsByRequestId = reviewsByRequestId;
        
        return View(requests);
    }

    // Export PDF de l'historique
    [HttpGet]
    public async Task<IActionResult> ExportHistoryPdf()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        var requests = await _patientService.GetPatientRequestsWithTimelineAsync(userId);

        // Generate HTML content for PDF
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><meta charset='utf-8'><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine("h1 { color: #0d6efd; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        html.AppendLine("th { background-color: #0d6efd; color: white; }");
        html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        html.AppendLine(".badge { padding: 4px 8px; border-radius: 4px; font-size: 12px; }");
        html.AppendLine(".completed { background-color: #198754; color: white; }");
        html.AppendLine(".cancelled { background-color: #6c757d; color: white; }");
        html.AppendLine(".in-progress { background-color: #0d6efd; color: white; }");
        html.AppendLine(".open { background-color: #ffc107; color: black; }");
        html.AppendLine("</style></head><body>");
        
        html.AppendLine($"<h1>Historique des demandes - MediAid</h1>");
        html.AppendLine($"<p><strong>Patient:</strong> {user?.FirstName} {user?.LastName}</p>");
        html.AppendLine($"<p><strong>Email:</strong> {user?.Email}</p>");
        html.AppendLine($"<p><strong>Date d'export:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
        
        html.AppendLine("<table>");
        html.AppendLine("<thead><tr>");
        html.AppendLine("<th>Date</th><th>Titre</th><th>CatÃ©gorie</th><th>Urgence</th><th>Statut</th>");
        html.AppendLine("</tr></thead><tbody>");

        foreach (var request in requests.OrderByDescending(r => r.CreatedAt))
        {
            var statusClass = request.Status.ToLower() switch
            {
                "completed" => "completed",
                "cancelled" => "cancelled",
                "inprogress" => "in-progress",
                _ => "open"
            };

            html.AppendLine("<tr>");
            html.AppendLine($"<td>{request.CreatedAt:dd/MM/yyyy HH:mm}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(request.Title)}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(request.Category)}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(request.Urgency)}</td>");
            html.AppendLine($"<td><span class='badge {statusClass}'>{WebUtility.HtmlEncode(request.Status)}</span></td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");
        html.AppendLine($"<p style='margin-top: 20px; font-size: 12px; color: #6c757d;'>Total: {requests.Count} demande(s)</p>");
        html.AppendLine("</body></html>");

        // Convert HTML to PDF using simple approach (for production, use a library like QuestPDF, iTextSharp, etc.)
        // For now, return HTML that can be printed to PDF by the browser
        var bytes = Encoding.UTF8.GetBytes(html.ToString());
        return File(bytes, "text/html", $"historique_mediaid_{DateTime.Now:yyyyMMdd_HHmmss}.html");
    }
}


