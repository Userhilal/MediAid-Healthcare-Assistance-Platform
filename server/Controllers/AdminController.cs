using MediAid.Data;
using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly IRequestService _requestService;
    private readonly IAuditLogService _auditLogService;
    private readonly MongoDbContext _context;

    public AdminController(IUserService userService, IRequestService requestService,
        IAuditLogService auditLogService, MongoDbContext context)
    {
        _userService = userService;
        _requestService = requestService;
        _auditLogService = auditLogService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Dashboard statistics
        var totalUsers = await _context.Users.CountDocumentsAsync(_ => true);
        var totalRequests = await _context.Requests.CountDocumentsAsync(_ => true);
        var activeAidants = await _context.Aidants.CountDocumentsAsync(a => a.AvailabilityStatus == "Available");
        var pendingRequests = await _context.Requests.CountDocumentsAsync(r => r.Status == "Open");

        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalRequests = totalRequests;
        ViewBag.ActiveAidants = activeAidants;
        ViewBag.PendingRequests = pendingRequests;

        return View();
    }

    public async Task<IActionResult> Users()
    {
        var users = await _context.Users.Find(_ => true)
            .SortByDescending(u => u.CreatedAt)
            .ToListAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendUser(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);

        TempData["SuccessMessage"] = "Utilisateur suspendu avec succès.";
        return RedirectToAction("Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateUser(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);

        TempData["SuccessMessage"] = "Utilisateur activé avec succès.";
        return RedirectToAction("Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string role)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);

        TempData["SuccessMessage"] = $"Rôle modifié en {role}.";
        return RedirectToAction("Users");
    }

    public async Task<IActionResult> AuditLogs(int limit = 100)
    {
        var logs = await _context.AuditLogs.Find(_ => true)
            .SortByDescending(l => l.CreatedAt)
            .Limit(limit)
            .ToListAsync();
        return View(logs);
    }
}

