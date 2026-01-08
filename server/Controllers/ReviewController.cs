using MediAid.DTOs;
using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MediAid.Data;
using MongoDB.Driver;

namespace MediAid.Controllers;

[Authorize(Policy = "PatientOnly")]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IRequestService _requestService;
    private readonly IUserService _userService;
    private readonly IAidantService _aidantService;

    public ReviewController(IReviewService reviewService, IRequestService requestService, IUserService userService, IAidantService aidantService)
    {
        _reviewService = reviewService;
        _requestService = requestService;
        _userService = userService;
        _aidantService = aidantService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(string requestId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(requestId);

        if (request == null || request.PatientId != userId)
        {
            return NotFound();
        }

        // Vérifier que la demande est complétée
        if (request.Status != "Completed")
        {
            TempData["ErrorMessage"] = "Vous ne pouvez évaluer que les demandes complétées.";
            return RedirectToAction("Index", "Request");
        }

        // Vérifier qu'il y a un aidant assigné
        if (string.IsNullOrEmpty(request.AssignedAidantId))
        {
            TempData["ErrorMessage"] = "Aucun aidant assigné à cette demande.";
            return RedirectToAction("Index", "Request");
        }

        // Vérifier si une review existe déjà
        var existingReview = await _reviewService.GetReviewByRequestIdAsync(requestId);
        if (existingReview != null)
        {
            TempData["InfoMessage"] = "Vous avez déjà évalué cette demande.";
            return RedirectToAction("Index", "Request");
        }

        var dto = new CreateReviewDto
        {
            RequestId = requestId,
            AidantId = request.AssignedAidantId
        };

        ViewBag.Request = request;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(dto.RequestId);

        if (request == null || request.PatientId != userId)
        {
            return NotFound();
        }

        // Vérifier que la demande est complétée
        if (request.Status != "Completed")
        {
            TempData["ErrorMessage"] = "Vous ne pouvez évaluer que les demandes complétées.";
            return RedirectToAction("Index", "Request");
        }

        // Vérifier qu'il y a un aidant assigné
        if (string.IsNullOrEmpty(request.AssignedAidantId) || request.AssignedAidantId != dto.AidantId)
        {
            TempData["ErrorMessage"] = "Aidant invalide.";
            return RedirectToAction("Index", "Request");
        }

        // Vérifier si une review existe déjà
        var existingReview = await _reviewService.GetReviewByRequestIdAsync(dto.RequestId);
        if (existingReview != null)
        {
            TempData["InfoMessage"] = "Vous avez déjà évalué cette demande.";
            return RedirectToAction("Index", "Request");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Request = request;
            return View(dto);
        }

        var review = await _reviewService.CreateReviewAsync(dto.RequestId, dto.AidantId, userId, dto.Rating, dto.Comment);
        
        if (review == null)
        {
            TempData["ErrorMessage"] = "Erreur lors de la création de l'évaluation.";
            ViewBag.Request = request;
            return View(dto);
        }

        TempData["SuccessMessage"] = "Évaluation créée avec succès !";
        return RedirectToAction("Index", "Request");
    }

    [HttpGet]
    public async Task<IActionResult> ByAidant(string aidantId)
    {
        if (string.IsNullOrEmpty(aidantId))
        {
            return NotFound();
        }

        var reviews = await _reviewService.GetReviewsByAidantIdAsync(aidantId);
        
        // Get aidant info
        var context = HttpContext.RequestServices.GetRequiredService<MediAid.Data.MongoDbContext>();
        var filter = Builders<MediAid.Models.Aidant>.Filter.Eq(a => a.Id, aidantId);
        var aidant = await context.Aidants.Find(filter).FirstOrDefaultAsync();
        
        if (aidant == null)
        {
            return NotFound();
        }

        var aidantUser = await _userService.GetUserByIdAsync(aidant.UserId);
        
        // Get patient info for each review
        var reviewsWithPatientInfo = new List<object>();
        foreach (var review in reviews)
        {
            var patient = await _userService.GetUserByIdAsync(review.PatientId);
            reviewsWithPatientInfo.Add(new
            {
                Review = review,
                PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Patient anonyme"
            });
        }

        // Get completed requests for current user that can be reviewed
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        List<MediAid.Models.Request> reviewableRequests = new List<MediAid.Models.Request>();
        
        if (!string.IsNullOrEmpty(userId))
        {
            var allRequests = await _requestService.GetRequestsByPatientIdAsync(userId);
            reviewableRequests = allRequests
                .Where(r => r.Status == "Completed" 
                    && r.AssignedAidantId == aidantId
                    && !string.IsNullOrEmpty(r.AssignedAidantId))
                .ToList();
            
            // Filter out requests that already have reviews
            var requestsWithReviews = new List<MediAid.Models.Request>();
            foreach (var req in reviewableRequests)
            {
                var existingReview = await _reviewService.GetReviewByRequestIdAsync(req.Id!);
                if (existingReview == null)
                {
                    requestsWithReviews.Add(req);
                }
            }
            reviewableRequests = requestsWithReviews;
        }

        ViewBag.Aidant = aidant;
        ViewBag.AidantUser = aidantUser;
        ViewBag.Reviews = reviewsWithPatientInfo;
        ViewBag.AverageRating = aidant.ReputationScore;
        ViewBag.TotalReviews = reviews.Count;
        ViewBag.ReviewableRequests = reviewableRequests;
        ViewBag.CurrentUserId = userId;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromAidantPage(CreateReviewDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(dto.RequestId);

        if (request == null || request.PatientId != userId)
        {
            TempData["ErrorMessage"] = "Demande introuvable ou vous n'avez pas les droits.";
            return RedirectToAction("ByAidant", new { aidantId = dto.AidantId });
        }

        // Vérifier que la demande est complétée
        if (request.Status != "Completed")
        {
            TempData["ErrorMessage"] = "Vous ne pouvez évaluer que les demandes complétées.";
            return RedirectToAction("ByAidant", new { aidantId = dto.AidantId });
        }

        // Vérifier qu'il y a un aidant assigné
        if (string.IsNullOrEmpty(request.AssignedAidantId) || request.AssignedAidantId != dto.AidantId)
        {
            TempData["ErrorMessage"] = "Aidant invalide.";
            return RedirectToAction("ByAidant", new { aidantId = dto.AidantId });
        }

        // Vérifier si une review existe déjà
        var existingReview = await _reviewService.GetReviewByRequestIdAsync(dto.RequestId);
        if (existingReview != null)
        {
            TempData["InfoMessage"] = "Vous avez déjà évalué cette demande.";
            return RedirectToAction("ByAidant", new { aidantId = dto.AidantId });
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Veuillez corriger les erreurs du formulaire.";
            return RedirectToAction("ByAidant", new { aidantId = dto.AidantId });
        }

        var review = await _reviewService.CreateReviewAsync(dto.RequestId, dto.AidantId, userId, dto.Rating, dto.Comment);
        
        if (review == null)
        {
            TempData["ErrorMessage"] = "Erreur lors de la création de l'évaluation.";
            return RedirectToAction("ByAidant", new { aidantId = dto.AidantId });
        }

        TempData["SuccessMessage"] = "Évaluation créée avec succès !";
        return RedirectToAction("ByAidant", new { aidantId = dto.AidantId });
    }
}

