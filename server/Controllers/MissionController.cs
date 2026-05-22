using MediAid.Data;
using MediAid.Models;
using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize]
public class MissionController : Controller
{
    private readonly MongoDbContext _context;
    private readonly IRequestService _requestService;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;

    public MissionController(MongoDbContext context, IRequestService requestService, 
        IUserService userService, INotificationService notificationService)
    {
        _context = context;
        _requestService = requestService;
        _userService = userService;
        _notificationService = notificationService;
    }

    // Generate verification code for a mission
    [HttpPost]
    public async Task<IActionResult> GenerateVerificationCode(string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(requestId);
        
        if (request == null || request.PatientId != userId)
        {
            return Json(new { success = false, message = "Demande non trouvÃ©e" });
        }

        // Generate 4-digit code
        var random = new Random();
        var code = random.Next(1000, 9999).ToString();
        
        request.VerificationCode = code;
        await _requestService.UpdateRequestAsync(request);

        return Json(new { success = true, code = code });
    }

    // Upload proof of delivery
    [HttpPost]
    [RequestSizeLimit(SafeFileUploadService.ProofMaxBytes)]
    public async Task<IActionResult> UploadProof(string requestId, IFormFile file, string proofType = "Photo")
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);

        if (aidant == null)
        {
            return Json(new { success = false, message = "Aidant non trouvé" });
        }

        var request = await _requestService.GetRequestByIdAsync(requestId);
        if (request == null || request.AssignedAidantId != aidant.Id)
        {
            return Json(new { success = false, message = "Mission non trouvée ou non assignée" });
        }

        if (request.Status != "InProgress" && request.Status != "Assigned")
        {
            return Json(new { success = false, message = "La mission ne peut pas recevoir de preuve dans son état actuel." });
        }

        var upload = await SafeFileUploadService.SaveAsync(
            file,
            "proofs",
            SafeFileUploadService.ProofAllowedExtensions,
            SafeFileUploadService.ProofMaxBytes);

        if (!upload.IsValid)
        {
            return Json(new { success = false, message = upload.ErrorMessage });
        }

        if (string.IsNullOrEmpty(request.VerificationCode))
        {
            var random = new Random();
            request.VerificationCode = random.Next(1000, 9999).ToString();
        }

        var normalizedProofType = string.IsNullOrWhiteSpace(proofType) ? "Photo" : proofType.Trim();

        if (normalizedProofType.Length > 40)
        {
            normalizedProofType = normalizedProofType[..40];
        }

        var proof = new MissionProof
        {
            RequestId = requestId,
            AidantId = aidant.Id!,
            ProofType = normalizedProofType,
            FileUrl = upload.RelativeUrl,
            FileName = upload.OriginalFileName,
            VerificationCode = request.VerificationCode
        };

        await _context.MissionProofs.InsertOneAsync(proof);

        if (request.Status == "InProgress" || request.Status == "Assigned")
        {
            request.Status = "Completed";
            request.CompletedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            await _requestService.UpdateRequestAsync(request);

            await _notificationService.CreateNotificationAsync(
                request.PatientId,
                "RequestCompleted",
                "Mission terminée",
                $"La mission « {request.Title} » a été complétée par l'aidant.",
                request.Id ?? requestId,
                "Request");
        }

        return Json(new
        {
            success = true,
            fileUrl = upload.RelativeUrl,
            fileName = upload.OriginalFileName,
            verificationCode = request.VerificationCode
        });
    }

    // Check-in with GPS location
    [HttpPost]
    public async Task<IActionResult> CheckIn(string requestId, double latitude, double longitude)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false, message = "Aidant non trouvÃ©" });
        }

        var request = await _requestService.GetRequestByIdAsync(requestId);
        if (request == null || request.AssignedAidantId != aidant.Id)
        {
            return Json(new { success = false, message = "Mission non trouvÃ©e" });
        }

        if (request.Location == null)
        {
            return Json(new { success = false, message = "Adresse de destination non disponible" });
        }

        // Calculate distance
        var distance = CalculateDistance(latitude, longitude, 
            request.Location.Coordinates[1], request.Location.Coordinates[0]);

        var isWithinRadius = distance <= 0.05; // 50 meters

        // Update request with aidant location (GeoJSON format: [longitude, latitude])
        request.AidantLocation = new Location
        {
            Type = "Point",
            Coordinates = new double[] { longitude, latitude }
        };
        request.LastCheckInAt = DateTime.UtcNow;
        request.IsAidantOnSite = isWithinRadius;
        await _requestService.UpdateRequestAsync(request);

        // Create check-in record
        var checkIn = new MissionCheckIn
        {
            RequestId = requestId,
            AidantId = aidant.Id!,
            Latitude = latitude,
            Longitude = longitude,
            DistanceFromDestination = distance * 1000, // Convert to meters
            IsWithinRadius = isWithinRadius,
            CheckInType = isWithinRadius ? "Arrival" : "InProgress"
        };

        await _context.MissionCheckIns.InsertOneAsync(checkIn);

        // Notify patient if within radius
        if (isWithinRadius)
        {
            await _notificationService.CreateNotificationAsync(
                request.PatientId,
                "Aidant sur place",
                "L'aidant est arrivÃ© Ã  votre adresse",
                "Mission",
                requestId
            );
        }

        return Json(new { 
            success = true, 
            distance = Math.Round(distance * 1000, 0),
            isWithinRadius = isWithinRadius 
        });
    }

    // Report safety incident
    [HttpPost]
    public async Task<IActionResult> ReportIncident(string requestId, string incidentType, 
        string severity, string description, double? latitude = null, double? longitude = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(requestId);
        
        if (request == null)
        {
            return Json(new { success = false, message = "Mission non trouvÃ©e" });
        }

        var incident = new SafetyIncident
        {
            RequestId = requestId,
            ReportedBy = userId,
            IncidentType = incidentType,
            Severity = severity,
            Description = description,
            Status = "Reported"
        };

        if (latitude.HasValue && longitude.HasValue)
        {
            incident.Location = new Location
            {
                Type = "Point",
                Coordinates = new double[] { longitude.Value, latitude.Value }
            };
        }

        await _context.SafetyIncidents.InsertOneAsync(incident);

        // Notify admins (you can add admin notification logic here)
        // For now, we'll just return success

        return Json(new { success = true, message = "Incident signalÃ© avec succÃ¨s" });
    }

    // Verify mission with code
    [HttpPost]
    public async Task<IActionResult> VerifyMission(string requestId, string code)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(requestId);
        
        if (request == null || request.PatientId != userId)
        {
            return Json(new { success = false, message = "Mission non trouvÃ©e" });
        }

        if (string.IsNullOrEmpty(request.VerificationCode) || request.VerificationCode != code)
        {
            return Json(new { success = false, message = "Code de vÃ©rification incorrect" });
        }

        // Find and verify proof
        var proof = await _context.MissionProofs
            .Find(p => p.RequestId == requestId && p.VerificationCode == code)
            .FirstOrDefaultAsync();

        if (proof != null)
        {
            proof.IsVerified = true;
            proof.VerifiedAt = DateTime.UtcNow;
            proof.VerifiedBy = userId;
            await _context.MissionProofs.ReplaceOneAsync(p => p.Id == proof.Id, proof);
        }

        // Complete the mission
        request.Status = "Completed";
        request.CompletedAt = DateTime.UtcNow;
        await _requestService.UpdateRequestAsync(request);

        // Delete ephemeral messages (images that auto-delete after mission completion)
        await DeleteEphemeralMessagesAsync(requestId);

        return Json(new { success = true, message = "Mission vÃ©rifiÃ©e et complÃ©tÃ©e" });
    }

    // Delete ephemeral messages after mission completion
    private async Task DeleteEphemeralMessagesAsync(string requestId)
    {
        var ephemeralMessages = await _context.Messages
            .Find(m => m.RequestId == requestId && m.IsEphemeral == true)
            .ToListAsync();

        foreach (var message in ephemeralMessages)
        {
            // Delete attachment files from storage (if using file storage)
            // For now, we'll just mark them as deleted in the database
            // In production, you'd also delete the actual files from storage
            
            // Remove ephemeral messages
            await _context.Messages.DeleteOneAsync(m => m.Id == message.Id);
        }
    }

    // Get aidant location for tracking
    [HttpGet]
    public async Task<IActionResult> GetAidantLocation(string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(requestId);
        
        if (request == null)
        {
            return Json(new { success = false });
        }

        // Check if user is patient or aidant
        var isPatient = request.PatientId == userId;
        var aidant = await _userService.GetAidantByUserIdAsync(userId);
        var isAidant = aidant != null && request.AssignedAidantId == aidant.Id;

        if (!isPatient && !isAidant)
        {
            return Json(new { success = false });
        }

        return Json(new { 
            success = true,
            aidantLocation = request.AidantLocation,
            destinationLocation = request.Location,
            isAidantOnSite = request.IsAidantOnSite,
            lastCheckInAt = request.LastCheckInAt
        });
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
        return degrees * (Math.PI / 180);
    }
}






