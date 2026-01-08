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
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.User = user;

        if (user.Role == "Aidant")
        {
            var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
            ViewBag.Aidant = aidant;
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, 
        string? profilePhoto, string? bio, string? skills)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _userService.UpdateUserAsync(user);

        // Update aidant profile if user is an aidant
        if (user.Role == "Aidant")
        {
            var aidant = await _aidantService.GetAidantByUserIdAsync(userId);
            if (aidant != null)
            {
                if (!string.IsNullOrEmpty(profilePhoto))
                {
                    aidant.ProfilePhoto = profilePhoto;
                }
                if (!string.IsNullOrEmpty(bio))
                {
                    aidant.Bio = bio;
                }
                if (!string.IsNullOrEmpty(skills))
                {
                    aidant.Skills = skills.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
                aidant.UpdatedAt = DateTime.UtcNow;
                await _aidantService.UpdateAidantAsync(aidant);
            }
        }

        TempData["SuccessMessage"] = "Profil mis à jour avec succès.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLocation(double latitude, double longitude, double radius)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await _aidantService.UpdateLocationAsync(userId, latitude, longitude, radius);
        
        if (result)
        {
            TempData["SuccessMessage"] = "Localisation mise à jour avec succès.";
        }
        else
        {
            TempData["ErrorMessage"] = "Erreur lors de la mise à jour de la localisation.";
        }

        return RedirectToAction("Index");
    }
}


