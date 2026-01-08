using MediAid.DTOs;
using MediAid.Models;

namespace MediAid.Services;

public interface IRequestService
{
    Task<Request?> CreateRequestAsync(string patientId, CreateRequestDto dto);
    Task<Request?> CreateRequestFromWizardAsync(string patientId, CreateRequestWizardDto dto);
    Task<Request?> GetRequestByIdAsync(string requestId);
    Task<List<Request>> GetRequestsByPatientIdAsync(string patientId);
    Task<List<Request>> GetAvailableRequestsAsync(double? latitude, double? longitude, double? radiusKm, string? category, string? urgency);
    Task<List<Request>> GetAllRequestsWithLocationAsync();
    Task<bool> UpdateRequestAsync(Request request);
    Task<bool> CancelRequestAsync(string requestId, string patientId);
    Task<bool> AssignAidantAsync(string requestId, string aidantId);
    Task<bool> DeleteRequestAsync(string requestId, string patientId);
}
