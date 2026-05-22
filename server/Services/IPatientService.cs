using MediAid.DTOs;
using MediAid.Models;

namespace MediAid.Services;

public interface IPatientService
{
    Task<Patient?> GetPatientByUserIdAsync(string userId);
    Task<bool> UpdatePatientProfileAsync(string userId, PatientProfileDto dto);
    Task<PatientDashboardStats> GetDashboardStatsAsync(string userId);
    Task<List<Request>> GetPatientRequestsWithTimelineAsync(string userId);
    Task<List<Request>> GetPatientRequestHistoryAsync(string userId);
    Task<bool> BlockAidantAsync(string userId, string aidantId);
    Task<bool> UnblockAidantAsync(string userId, string aidantId);
    Task<bool> IsAidantBlockedAsync(string userId, string aidantId);
}

public class PatientDashboardStats
{
    public int ActiveRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int TotalRequests { get; set; }
    public Request? CurrentActiveRequest { get; set; }
    public Aidant? AssignedAidant { get; set; }
    public int UnreadNotifications { get; set; }
}


