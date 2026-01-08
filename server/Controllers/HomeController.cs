using Microsoft.AspNetCore.Mvc;
using MediAid.Data;
using MediAid.Services;
using MongoDB.Driver;

namespace MediAid.Controllers;

public class HomeController : Controller
{
    private readonly MongoDbContext _context;
    private readonly IReviewService _reviewService;

    public HomeController(MongoDbContext context, IReviewService reviewService)
    {
        _context = context;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index()
    {
        // Get MongoDB context from service provider
        var context = HttpContext.RequestServices.GetRequiredService<MongoDbContext>();
        
        // Get real statistics from database
        var totalAidants = await context.Aidants.CountDocumentsAsync(FilterDefinition<MediAid.Models.Aidant>.Empty);
        var availableAidants = await context.Aidants.CountDocumentsAsync(
            Builders<MediAid.Models.Aidant>.Filter.Eq(a => a.AvailabilityStatus, "Available")
        );
        
        var totalPatients = await context.Patients.CountDocumentsAsync(FilterDefinition<MediAid.Models.Patient>.Empty);
        var totalUsers = await context.Users.CountDocumentsAsync(FilterDefinition<MediAid.Models.User>.Empty);
        
        // Get completed requests (patients helped)
        var completedRequests = await context.Requests.CountDocumentsAsync(
            Builders<MediAid.Models.Request>.Filter.Eq(r => r.Status, "Completed")
        );
        
        // Calculate average rating from all reviews
        var allReviews = await context.Reviews.Find(FilterDefinition<MediAid.Models.Review>.Empty).ToListAsync();
        var averageRating = allReviews.Count > 0 
            ? Math.Round(allReviews.Average(r => r.Rating), 1) 
            : 0.0;
        
        // Get unique patients who have been helped (have at least one completed request)
        var uniquePatientsHelped = await context.Requests
            .Distinct(r => r.PatientId, Builders<MediAid.Models.Request>.Filter.Eq(r => r.Status, "Completed"))
            .ToListAsync();
        var uniquePatientsCount = uniquePatientsHelped.Count;

        // Calculate total volunteer hours from completed requests
        var completedRequestsList = await context.Requests
            .Find(Builders<MediAid.Models.Request>.Filter.Eq(r => r.Status, "Completed"))
            .ToListAsync();
        
        double totalVolunteerHours = 0.0;
        foreach (var req in completedRequestsList)
        {
            if (req.RequestedDate.HasValue)
            {
                var endDate = req.CompletedAt ?? req.UpdatedAt;
                var duration = endDate - req.RequestedDate.Value;
                if (duration.TotalHours > 0 && duration.TotalHours < 24)
                {
                    totalVolunteerHours += duration.TotalHours;
                }
                else
                {
                    totalVolunteerHours += 1.0; // Default 1 hour per mission
                }
            }
            else
            {
                totalVolunteerHours += 1.0; // Default 1 hour per mission
            }
        }

        // Also sum from aidants' TotalHours if available
        var aidantsWithHours = await context.Aidants
            .Find(FilterDefinition<MediAid.Models.Aidant>.Empty)
            .ToListAsync();
        var totalHoursFromAidants = aidantsWithHours.Sum(a => a.TotalHours);
        
        // Use the maximum of the two calculations
        var finalTotalHours = Math.Max(totalVolunteerHours, totalHoursFromAidants);

        ViewBag.TotalAidants = (int)totalAidants;
        ViewBag.AvailableAidants = (int)availableAidants;
        ViewBag.TotalPatients = (int)totalPatients;
        ViewBag.TotalUsers = (int)totalUsers;
        ViewBag.PatientsHelped = uniquePatientsCount;
        ViewBag.CompletedRequests = (int)completedRequests;
        ViewBag.AverageRating = averageRating;
        ViewBag.TotalVolunteerHours = (int)Math.Round(finalTotalHours);

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}


