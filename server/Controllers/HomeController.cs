using MediAid.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediAid.Controllers;

public class HomeController : Controller
{
    private readonly IPlatformStatsService _platformStatsService;

    public HomeController(IPlatformStatsService platformStatsService)
    {
        _platformStatsService = platformStatsService;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _platformStatsService.GetStatsAsync();

        ViewBag.TotalAidants = stats.TotalAidants;
        ViewBag.AvailableAidants = stats.AvailableAidants;
        ViewBag.TotalPatients = stats.TotalPatients;
        ViewBag.TotalUsers = stats.TotalUsers;
        ViewBag.PatientsHelped = stats.PatientsHelped;
        ViewBag.CompletedRequests = stats.CompletedRequests;
        ViewBag.OpenRequests = stats.OpenRequests;
        ViewBag.AverageRating = stats.AverageRating;
        ViewBag.TotalVolunteerHours = stats.TotalVolunteerHours;
        ViewBag.IsDatabaseAvailable = stats.IsDatabaseAvailable;

        return View(stats);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}

