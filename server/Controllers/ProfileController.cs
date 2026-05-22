using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IAidantService _aidantService;

    public ProfileController(IUserService userService, IAidantService aidantService)
    {
        _userService = userService;
        _aidantService = aidantService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = firstName?.Trim();
        user.LastName = lastName?.Trim();
        user.PhoneNumber = phoneNumber?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _userService.UpdateUserAsync(user);

        TempData["SuccessMessage"] = "Votre compte a été mis à jour avec succès.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLocation(double latitude, double longitude, double radius)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var aidant = await _aidantService.GetAidantByUserIdAsync(userId);

        if (aidant == null)
        {
            TempData["ErrorMessage"] = "La localisation est disponible uniquement pour les profils aidants.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _aidantService.UpdateLocationAsync(userId, latitude, longitude, radius);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] =
            result
                ? "Localisation mise à jour avec succès."
                : "Erreur lors de la mise à jour de la localisation.";

        return RedirectToAction("Profile", "Aidant");
    }
}


