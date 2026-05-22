using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;
using System.Text;

namespace MediAid.Services;

public class PlanningService : IPlanningService
{
    private readonly MongoDbContext _context;

    public PlanningService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Planning?> GetPlanningByDateAsync(string aidantId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Plannings
            .Find(p => p.AidantId == aidantId && p.Date >= startOfDay && p.Date < endOfDay)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Planning>> GetPlanningByDateRangeAsync(string aidantId, DateTime startDate, DateTime endDate)
    {
        return await _context.Plannings
            .Find(p => p.AidantId == aidantId && p.Date >= startDate.Date && p.Date <= endDate.Date)
            .SortBy(p => p.Date)
            .ToListAsync();
    }

    public async Task<bool> AddTimeSlotAsync(string aidantId, DateTime date, PlanningTimeSlot timeSlot)
    {
        // Vérifier les conflits
        var conflicts = await CheckConflictsAsync(aidantId, date, timeSlot.StartTime, timeSlot.EndTime);
        if (conflicts.Any(c => c.Type == "Overlap" || c.Type == "Mission"))
        {
            return false;
        }

        var planning = await GetPlanningByDateAsync(aidantId, date);
        
        if (planning == null)
        {
            planning = new Planning
            {
                AidantId = aidantId,
                Date = date.Date,
                TimeSlots = new List<PlanningTimeSlot> { timeSlot }
            };
            await _context.Plannings.InsertOneAsync(planning);
        }
        else
        {
            planning.TimeSlots.Add(timeSlot);
            planning.UpdatedAt = DateTime.UtcNow;
            await _context.Plannings.ReplaceOneAsync(p => p.Id == planning.Id, planning);
        }

        return true;
    }

    public async Task<bool> UpdateTimeSlotAsync(string aidantId, DateTime date, string timeSlotId, PlanningTimeSlot updatedSlot)
    {
        var planning = await GetPlanningByDateAsync(aidantId, date);
        if (planning == null) return false;

        // Parse the slotId to find the matching slot
        var slotParts = timeSlotId.Split('-');
        if (slotParts.Length < 3) return false;
        
        var slot = planning.TimeSlots.FirstOrDefault(s => 
            s.StartTime.ToString(@"hh\:mm") == slotParts[0] && 
            s.EndTime.ToString(@"hh\:mm") == slotParts[1] && 
            s.Type == slotParts[2]);
        
        if (slot == null) return false;

        // Vérifier les conflits avec les autres créneaux
        var conflicts = planning.TimeSlots
            .Where(s => !(s.StartTime.ToString(@"hh\:mm") == slotParts[0] && s.EndTime.ToString(@"hh\:mm") == slotParts[1] && s.Type == slotParts[2]))
            .Where(s => HasOverlap(updatedSlot.StartTime, updatedSlot.EndTime, s.StartTime, s.EndTime))
            .Select(s => new PlanningConflict
            {
                Type = s.Type == "Mission" ? "Mission" : "Overlap",
                ConflictingSlot = s,
                Message = $"Conflit avec un créneau {s.Type}"
            })
            .ToList();

        if (conflicts.Any())
        {
            return false;
        }

        slot.StartTime = updatedSlot.StartTime;
        slot.EndTime = updatedSlot.EndTime;
        slot.Type = updatedSlot.Type;
        slot.Title = updatedSlot.Title;
        slot.Description = updatedSlot.Description;
        slot.RequestId = updatedSlot.RequestId;

        planning.UpdatedAt = DateTime.UtcNow;
        var result = await _context.Plannings.ReplaceOneAsync(p => p.Id == planning.Id, planning);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> RemoveTimeSlotAsync(string aidantId, DateTime date, string timeSlotId)
    {
        var planning = await GetPlanningByDateAsync(aidantId, date);
        if (planning == null) return false;

        // Parse the slotId to find the matching slot
        var slotParts = timeSlotId.Split('-');
        if (slotParts.Length < 3) return false;
        
        var slot = planning.TimeSlots.FirstOrDefault(s => 
            s.StartTime.ToString(@"hh\:mm") == slotParts[0] && 
            s.EndTime.ToString(@"hh\:mm") == slotParts[1] && 
            s.Type == slotParts[2]);
        
        if (slot == null) return false;

        // Ne pas permettre la suppression d'une mission assignée
        if (slot.Type == "Mission" && !string.IsNullOrEmpty(slot.RequestId))
        {
            return false;
        }

        planning.TimeSlots.Remove(slot);
        planning.UpdatedAt = DateTime.UtcNow;

        // Si plus de créneaux, supprimer le planning
        if (!planning.TimeSlots.Any())
        {
            var deleteResult = await _context.Plannings.DeleteOneAsync(p => p.Id == planning.Id);
            return deleteResult.DeletedCount > 0;
        }

        var result = await _context.Plannings.ReplaceOneAsync(p => p.Id == planning.Id, planning);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> BlockTimeSlotAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime, string? reason = null)
    {
        var slot = new PlanningTimeSlot
        {
            StartTime = startTime,
            EndTime = endTime,
            Type = "Blocked",
            Title = "Créneau bloqué",
            Description = reason
        };

        return await AddTimeSlotAsync(aidantId, date, slot);
    }

    public async Task<bool> AssignMissionToSlotAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime, string requestId, string? title = null)
    {
        var slot = new PlanningTimeSlot
        {
            StartTime = startTime,
            EndTime = endTime,
            Type = "Mission",
            RequestId = requestId,
            Title = title ?? "Mission"
        };

        return await AddTimeSlotAsync(aidantId, date, slot);
    }

    public async Task<List<PlanningTimeSlot>> GetAvailableSlotsAsync(string aidantId, DateTime date)
    {
        var planning = await GetPlanningByDateAsync(aidantId, date);
        if (planning == null) return new List<PlanningTimeSlot>();

        return planning.TimeSlots
            .Where(s => s.Type == "Available")
            .OrderBy(s => s.StartTime)
            .ToList();
    }

    public async Task<bool> IsSlotAvailableAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var conflicts = await CheckConflictsAsync(aidantId, date, startTime, endTime);
        return !conflicts.Any();
    }

    public async Task<List<PlanningConflict>> CheckConflictsAsync(string aidantId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var planning = await GetPlanningByDateAsync(aidantId, date);
        if (planning == null) return new List<PlanningConflict>();

        var conflicts = new List<PlanningConflict>();

        foreach (var slot in planning.TimeSlots)
        {
            if (HasOverlap(startTime, endTime, slot.StartTime, slot.EndTime))
            {
                conflicts.Add(new PlanningConflict
                {
                    Type = slot.Type == "Mission" ? "Mission" : slot.Type == "Blocked" ? "Blocked" : "Overlap",
                    ConflictingSlot = slot,
                    Message = slot.Type == "Mission" 
                        ? $"Conflit avec une mission: {slot.Title}" 
                        : slot.Type == "Blocked"
                        ? "Créneau bloqué"
                        : "Conflit avec un autre créneau"
                });
            }
        }

        return conflicts;
    }

    private bool HasOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
    {
        return start1 < end2 && end1 > start2;
    }
}


