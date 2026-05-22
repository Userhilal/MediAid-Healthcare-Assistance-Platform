using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUserService _userService;
    private readonly IRequestService _requestService;
    private readonly INotificationService _notificationService;

    public DashboardController(IUserService userService, IRequestService requestService, INotificationService notificationService)
    {
        _userService = userService;
        _requestService = requestService;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.User = user;
        ViewBag.Role = user.Role;

        // Get notifications
        var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, unreadOnly: true);
        ViewBag.UnreadNotifications = notifications.Count;

        // Redirect to role-specific dashboard
        return user.Role switch
        {
            "Patient" => RedirectToAction("Dashboard", "Patient"),
            "Aidant" => RedirectToAction("Dashboard", "Aidant"),
            "Expert" => RedirectToAction("Index", "Expert"),
            "Admin" => RedirectToAction("Index", "Admin"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}



