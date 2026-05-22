using MediAid.Models;
using MediAid.Services;
using MediAid.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize(Policy = "AidantOnly")]
public class PlanningController : Controller
{
    private readonly IPlanningService _planningService;
    private readonly IAidantService _aidantService;
    private readonly IUserService _userService;
    private readonly IRequestService _requestService;

    public PlanningController(
        IPlanningService planningService,
        IAidantService aidantService,
        IUserService userService,
        IRequestService requestService)
    {
        _planningService = planningService;
        _aidantService = aidantService;
        _userService = userService;
        _requestService = requestService;
    }

    // Vue principale du planning (calendrier)
    public async Task<IActionResult> Index(DateTime? date = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return RedirectToAction("Profile", "Aidant");
        }

        var targetDate = date ?? DateTime.Today;
        var startOfWeek = targetDate.AddDays(-(int)targetDate.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);

        var planning = await _planningService.GetPlanningByDateRangeAsync(aidant.Id!, startOfWeek, endOfWeek);
        var user = await _userService.GetUserByIdAsync(userId);

        // Synchroniser les missions existantes avec le planning
        await SyncMissionsToPlanningAsync(aidant.Id!);

        // Recharger le planning aprÃ¨s synchronisation
        planning = await _planningService.GetPlanningByDateRangeAsync(aidant.Id!, startOfWeek, endOfWeek);

        ViewBag.Aidant = aidant;
        ViewBag.User = user;
        ViewBag.CurrentDate = targetDate;
        ViewBag.StartOfWeek = startOfWeek;
        ViewBag.EndOfWeek = endOfWeek;
        ViewBag.Plannings = planning;

        return View();
    }

    // API: Ajouter un crÃ©neau disponible
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAvailableSlot(DateTime date, string startTime, string endTime)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false, message = "Aidant non trouvÃ©" });
        }

        if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
        {
            return Json(new { success = false, message = "Format d'heure invalide" });
        }

        var slot = new PlanningTimeSlot
        {
            StartTime = start,
            EndTime = end,
            Type = "Available"
        };

        var success = await _planningService.AddTimeSlotAsync(aidant.Id!, date, slot);
        
        if (!success)
        {
            return Json(new { success = false, message = "Conflit avec un autre crÃ©neau" });
        }

        return Json(new { success = true, message = "CrÃ©neau ajoutÃ© avec succÃ¨s" });
    }

    // API: Bloquer un crÃ©neau
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockSlot(DateTime date, string startTime, string endTime, string? reason = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false, message = "Aidant non trouvÃ©" });
        }

        if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
        {
            return Json(new { success = false, message = "Format d'heure invalide" });
        }

        var success = await _planningService.BlockTimeSlotAsync(aidant.Id!, date, start, end, reason);
        
        if (!success)
        {
            return Json(new { success = false, message = "Conflit avec un autre crÃ©neau" });
        }

        return Json(new { success = true, message = "CrÃ©neau bloquÃ© avec succÃ¨s" });
    }

    // API: Assigner une mission Ã  un crÃ©neau
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMission(DateTime date, string startTime, string endTime, string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false, message = "Aidant non trouvÃ©" });
        }

        var request = await _requestService.GetRequestByIdAsync(requestId);
        if (request == null)
        {
            return Json(new { success = false, message = "Demande non trouvÃ©e" });
        }

        if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
        {
            return Json(new { success = false, message = "Format d'heure invalide" });
        }

        var success = await _planningService.AssignMissionToSlotAsync(
            aidant.Id!, 
            date, 
            start, 
            end, 
            requestId, 
            request.Title);

        if (!success)
        {
            return Json(new { success = false, message = "Conflit avec un autre crÃ©neau" });
        }

        return Json(new { success = true, message = "Mission assignÃ©e au planning" });
    }

    // API: Supprimer un crÃ©neau
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSlot(DateTime date, string slotId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false, message = "Aidant non trouvÃ©" });
        }

        var success = await _planningService.RemoveTimeSlotAsync(aidant.Id!, date, slotId);
        
        if (!success)
        {
            return Json(new { success = false, message = "Impossible de supprimer ce crÃ©neau" });
        }

        return Json(new { success = true, message = "CrÃ©neau supprimÃ©" });
    }

    // API: Obtenir le planning d'une date
    [HttpGet]
    public async Task<IActionResult> GetPlanning(DateTime date)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false });
        }

        var planning = await _planningService.GetPlanningByDateAsync(aidant.Id!, date);
        
        return Json(new { 
            success = true, 
            planning = planning?.TimeSlots.Select(s => new {
                id = $"{s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}-{s.Type}",
                startTime = s.StartTime.ToString(@"hh\:mm"),
                endTime = s.EndTime.ToString(@"hh\:mm"),
                type = s.Type,
                title = s.Title,
                description = s.Description,
                requestId = s.RequestId
            })
        });
    }

    // API: VÃ©rifier la disponibilitÃ©
    [HttpGet]
    public async Task<IActionResult> CheckAvailability(DateTime date, string startTime, string endTime)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false, available = false });
        }

        if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
        {
            return Json(new { success = false, available = false });
        }

        var isAvailable = await _planningService.IsSlotAvailableAsync(aidant.Id!, date, start, end);
        var conflicts = await _planningService.CheckConflictsAsync(aidant.Id!, date, start, end);

        return Json(new { 
            success = true, 
            available = isAvailable,
            conflicts = conflicts.Select(c => new {
                type = c.Type,
                message = c.Message
            })
        });
    }

    // API: Synchroniser les missions avec le planning
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncMissions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
        
        if (aidant == null)
        {
            return Json(new { success = false, message = "Aidant non trouvÃ©" });
        }

        await SyncMissionsToPlanningAsync(aidant.Id!);
        
        return Json(new { success = true, message = "Missions synchronisÃ©es avec succÃ¨s" });
    }

    // Synchroniser les missions acceptÃ©es avec le planning
    private async Task SyncMissionsToPlanningAsync(string aidantId)
    {
        // RÃ©cupÃ©rer toutes les demandes assignÃ©es Ã  cet aidant
        var context = HttpContext.RequestServices.GetRequiredService<MongoDbContext>();
        var filter = Builders<Request>.Filter.And(
            Builders<Request>.Filter.Eq(r => r.AssignedAidantId, aidantId),
            Builders<Request>.Filter.In(r => r.Status, new[] { "Assigned", "InProgress" })
        );
        var assignedRequests = await context.Requests
            .Find(filter)
            .ToListAsync();

        foreach (var request in assignedRequests)
        {
            if (request.RequestedDate.HasValue)
            {
                var missionDate = request.RequestedDate.Value.Date;
                var startTime = request.RequestedDate.Value.TimeOfDay;
                var endTime = startTime.Add(TimeSpan.FromHours(1)); // DurÃ©e par dÃ©faut 1h

                // VÃ©rifier si la mission existe dÃ©jÃ  dans le planning
                var planning = await _planningService.GetPlanningByDateAsync(aidantId, missionDate);
                var missionExists = planning?.TimeSlots.Any(s => 
                    s.Type == "Mission" && s.RequestId == request.Id) ?? false;

                if (!missionExists)
                {
                    // Ajouter la mission au planning
                    await _planningService.AssignMissionToSlotAsync(
                        aidantId,
                        missionDate,
                        startTime,
                        endTime,
                        request.Id!,
                        request.Title
                    );
                }
            }
        }
    }
}


