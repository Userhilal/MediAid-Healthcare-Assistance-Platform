using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAid.Controllers;

[Authorize]
public class MapController : Controller
{
    private readonly IAidantService _aidantService;
    private readonly IRequestService _requestService;
    private readonly IUserService _userService;

    public MapController(IAidantService aidantService, IRequestService requestService, IUserService userService)
    {
        _aidantService = aidantService;
        _requestService = requestService;
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        var aidants = await _aidantService.GetAllAidantsWithLocationAsync();
        var requests = await _requestService.GetAllRequestsWithLocationAsync();

        // Enrichir les aidants avec les informations utilisateur
        var aidantsWithUserInfo = new List<object>();
        foreach (var aidant in aidants)
        {
            var user = await _userService.GetUserByIdAsync(aidant.UserId);
            if (user != null && aidant.Location != null && aidant.Location.Coordinates.Length >= 2)
            {
                aidantsWithUserInfo.Add(new
                {
                    Id = aidant.Id,
                    UserId = aidant.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Bio = aidant.Bio,
                    Latitude = aidant.Location.Coordinates[1],
                    Longitude = aidant.Location.Coordinates[0],
                    InterventionRadius = aidant.InterventionRadius,
                    ReputationScore = aidant.ReputationScore,
                    AvailabilityStatus = aidant.AvailabilityStatus
                });
            }
        }

        // Enrichir les demandes avec les informations patient
        var requestsWithUserInfo = new List<object>();
        foreach (var request in requests)
        {
            if (request.Location != null && request.Location.Coordinates.Length >= 2)
            {
                var patient = await _userService.GetPatientByUserIdAsync(request.PatientId);
                var user = await _userService.GetUserByIdAsync(request.PatientId);
                
                requestsWithUserInfo.Add(new
                {
                    Id = request.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Category = request.Category,
                    Urgency = request.Urgency,
                    Latitude = request.Location.Coordinates[1],
                    Longitude = request.Location.Coordinates[0],
                    Address = request.Address,
                    City = request.City,
                    Status = request.Status,
                    PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Inconnu"
                });
            }
        }

        ViewBag.Aidants = aidantsWithUserInfo;
        ViewBag.Requests = requestsWithUserInfo;

        return View();
    }
}

