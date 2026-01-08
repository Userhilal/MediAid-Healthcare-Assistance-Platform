using MediAid.Data;
using MediAid.DTOs;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class PatientService : IPatientService
{
    private readonly MongoDbContext _context;
    private readonly IUserService _userService;
    private readonly IRequestService _requestService;
    private readonly INotificationService _notificationService;

    public PatientService(MongoDbContext context, IUserService userService, 
        IRequestService requestService, INotificationService notificationService)
    {
        _context = context;
        _userService = userService;
        _requestService = requestService;
        _notificationService = notificationService;
    }

    public async Task<Patient?> GetPatientByUserIdAsync(string userId)
    {
        return await _context.Patients.Find(p => p.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<bool> UpdatePatientProfileAsync(string userId, PatientProfileDto dto)
    {
        var patient = await GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            // Create patient profile if it doesn't exist
            patient = new Patient
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // Update patient fields
        patient.ProfilePhoto = dto.ProfilePhoto;
        patient.DateOfBirth = dto.DateOfBirth;
        patient.Address = dto.Address;
        patient.City = dto.City;
        patient.PostalCode = dto.PostalCode;
        patient.LocationBlurRadius = dto.LocationBlurRadius;
        patient.ContactPreference = dto.ContactPreference;
        patient.AnonymousMode = dto.AnonymousMode;
        patient.MedicalConditions = dto.MedicalConditions;
        patient.UpdatedAt = DateTime.UtcNow;

        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            patient.Location = new Location
            {
                Type = "Point",
                Coordinates = new double[] { dto.Longitude.Value, dto.Latitude.Value }
            };
        }

        if (dto.EmergencyContact != null)
        {
            patient.EmergencyContact = new EmergencyContact
            {
                Name = dto.EmergencyContact.Name,
                PhoneNumber = dto.EmergencyContact.PhoneNumber,
                Relationship = dto.EmergencyContact.Relationship
            };
        }

        // Update user fields
        var user = await _userService.GetUserByIdAsync(userId);
        if (user != null)
        {
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user);
        }

        if (patient.Id == null)
        {
            await _context.Patients.InsertOneAsync(patient);
        }
        else
        {
            await _context.Patients.ReplaceOneAsync(p => p.Id == patient.Id, patient);
        }

        return true;
    }

    public async Task<PatientDashboardStats> GetDashboardStatsAsync(string userId)
    {
        var requests = await _requestService.GetRequestsByPatientIdAsync(userId);
        
        var activeRequests = requests.Where(r => r.Status != "Completed" && r.Status != "Cancelled").ToList();
        var completedRequests = requests.Where(r => r.Status == "Completed").ToList();
        var currentActive = activeRequests.FirstOrDefault(r => r.Status == "Assigned" || r.Status == "InProgress");

        Aidant? assignedAidant = null;
        if (currentActive?.AssignedAidantId != null)
        {
            var aidant = await _context.Aidants.Find(a => a.Id == currentActive.AssignedAidantId).FirstOrDefaultAsync();
            assignedAidant = aidant;
        }

        var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, unreadOnly: true);

        return new PatientDashboardStats
        {
            ActiveRequests = activeRequests.Count,
            CompletedRequests = completedRequests.Count,
            TotalRequests = requests.Count,
            CurrentActiveRequest = currentActive,
            AssignedAidant = assignedAidant,
            UnreadNotifications = notifications.Count
        };
    }

    public async Task<List<Request>> GetPatientRequestsWithTimelineAsync(string userId)
    {
        return await _requestService.GetRequestsByPatientIdAsync(userId);
    }

    public async Task<List<Request>> GetPatientRequestHistoryAsync(string userId)
    {
        return await _requestService.GetRequestsByPatientIdAsync(userId);
    }

    public async Task<bool> BlockAidantAsync(string userId, string aidantId)
    {
        var patient = await GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            return false;
        }

        if (patient.BlockedAidantIds == null)
        {
            patient.BlockedAidantIds = new List<string>();
        }

        if (!patient.BlockedAidantIds.Contains(aidantId))
        {
            patient.BlockedAidantIds.Add(aidantId);
            patient.UpdatedAt = DateTime.UtcNow;
            await _context.Patients.ReplaceOneAsync(p => p.Id == patient.Id, patient);
        }

        return true;
    }

    public async Task<bool> UnblockAidantAsync(string userId, string aidantId)
    {
        var patient = await GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            return false;
        }

        if (patient.BlockedAidantIds != null && patient.BlockedAidantIds.Contains(aidantId))
        {
            patient.BlockedAidantIds.Remove(aidantId);
            patient.UpdatedAt = DateTime.UtcNow;
            await _context.Patients.ReplaceOneAsync(p => p.Id == patient.Id, patient);
        }

        return true;
    }

    public async Task<bool> IsAidantBlockedAsync(string userId, string aidantId)
    {
        var patient = await GetPatientByUserIdAsync(userId);
        if (patient == null || patient.BlockedAidantIds == null)
        {
            return false;
        }

        return patient.BlockedAidantIds.Contains(aidantId);
    }
}

