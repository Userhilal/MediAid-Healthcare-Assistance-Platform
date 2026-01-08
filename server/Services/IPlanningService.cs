using MediAid.Models;

namespace MediAid.Services;

public interface IPlanningService
{
    Task<Planning?> GetPlanningByDateAsync(string aidantId, DateTime date);
    Task<List<Planning>> GetPlanningByDateRangeAsync(string aidantId, DateTime startDate, DateTime endDate);
    Task<bool> AddTimeSlotAsync(string aidantId, DateTime date, PlanningTimeSlot timeSlot);
    Task<bool> UpdateTimeSlotAsync(string aidantId, DateTime date, string timeSlotId, PlanningTimeSlot updatedSlot);
    Task<bool> RemoveTimeSlotAsync(string aidantId, DateTime date, string timeSlotId);
    Task<bool> BlockTimeSlotAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime, string? reason = null);
    Task<bool> AssignMissionToSlotAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime, string requestId, string? title = null);
    Task<List<PlanningTimeSlot>> GetAvailableSlotsAsync(string aidantId, DateTime date);
    Task<bool> IsSlotAvailableAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime);
    Task<List<PlanningConflict>> CheckConflictsAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime);
}

public class PlanningConflict
{
    public string Type { get; set; } = string.Empty; // Overlap, Mission, Blocked
    public PlanningTimeSlot? ConflictingSlot { get; set; }
    public string Message { get; set; } = string.Empty;
}




