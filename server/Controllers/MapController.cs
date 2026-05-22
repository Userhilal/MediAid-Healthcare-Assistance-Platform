using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentAidant = !string.IsNullOrWhiteSpace(currentUserId)
            ? await _userService.GetAidantByUserIdAsync(currentUserId)
            : null;

        var aidants = await _aidantService.GetAllAidantsWithLocationAsync();
        var requests = await _requestService.GetAllRequestsWithLocationAsync();

        var aidantsWithUserInfo = new List<object>();

        foreach (var aidant in aidants)
        {
            var user = await _userService.GetUserByIdAsync(aidant.UserId);

            if (user != null && aidant.Location?.Coordinates?.Length >= 2)
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

        var requestsWithUserInfo = new List<object>();

        foreach (var request in requests)
        {
            if (request.Location?.Coordinates == null || request.Location.Coordinates.Length < 2)
            {
                continue;
            }

            if (request.RequiresExpertValidation && !request.IsExpertValidated)
            {
                continue;
            }

            var canSeeExactLocation =
                User.IsInRole("Admin") ||
                request.PatientId == currentUserId ||
                (!string.IsNullOrWhiteSpace(request.AssignedAidantId) &&
                 currentAidant != null &&
                 request.AssignedAidantId == currentAidant.Id);

            var latitude = request.Location.Coordinates[1];
            var longitude = request.Location.Coordinates[0];

            if (!canSeeExactLocation)
            {
                (latitude, longitude) = BlurLocation(latitude, longitude, request.Id ?? request.Title);
            }

            requestsWithUserInfo.Add(new
            {
                Id = request.Id,
                Title = request.Title,
                Description = canSeeExactLocation ? request.Description : "Description masquée avant acceptation de la mission.",
                Category = request.Category,
                Urgency = request.Urgency,
                Latitude = latitude,
                Longitude = longitude,
                Address = canSeeExactLocation ? request.Address : "Zone approximative",
                City = request.City,
                Status = request.Status,
                PatientName = canSeeExactLocation ? "Patient identifié" : "Patient anonyme",
                IsApproximate = !canSeeExactLocation
            });
        }

        ViewBag.Aidants = aidantsWithUserInfo;
        ViewBag.Requests = requestsWithUserInfo;

        return View();
    }

    private static (double Latitude, double Longitude) BlurLocation(double latitude, double longitude, string seed)
    {
        var hash = Math.Abs(seed.GetHashCode());
        var latOffset = ((hash % 7) - 3) * 0.0015;
        var lonOffset = (((hash / 10) % 7) - 3) * 0.0015;

        return (latitude + latOffset, longitude + lonOffset);
    }
}


