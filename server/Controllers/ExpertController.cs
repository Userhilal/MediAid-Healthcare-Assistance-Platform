using MediAid.Data;
using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize(Policy = "ExpertOnly")]
public class ExpertController : Controller
{
    private readonly IRequestService _requestService;
    private readonly IUserService _userService;
    private readonly MongoDbContext _context;

    public ExpertController(IRequestService requestService, IUserService userService, MongoDbContext context)
    {
        _requestService = requestService;
        _userService = userService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var requests = await _context.Requests.Find(r => r.RequiresExpertValidation && !r.IsExpertValidated)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Validate(string id)
    {
        var request = await _requestService.GetRequestByIdAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate(string id, string recommendations)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        request.IsExpertValidated = true;
        request.ExpertId = userId;
        request.ExpertRecommendations = recommendations;
        request.UpdatedAt = DateTime.UtcNow;

        await _requestService.UpdateRequestAsync(request);

        TempData["SuccessMessage"] = "Demande validée avec succès.";
        return RedirectToAction("Index");
    }
}



