using MediAid.DTOs;
using MediAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediAid.Controllers;

[Authorize(Policy = "PatientOnly")]
public class RequestController : Controller
{
    private readonly IRequestService _requestService;
    private readonly IProposalService _proposalService;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly IProposalRecommendationService _recommendationService;
    private readonly IPatientService _patientService;

    public RequestController(IRequestService requestService, IProposalService proposalService, 
        IUserService userService, INotificationService notificationService,
        IProposalRecommendationService recommendationService, IPatientService patientService)
    {
        _requestService = requestService;
        _proposalService = proposalService;
        _userService = userService;
        _notificationService = notificationService;
        _recommendationService = recommendationService;
        _patientService = patientService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _userService.GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            return RedirectToAction("Register", "Account");
        }

        var requests = await _requestService.GetRequestsByPatientIdAsync(userId);
        return View(requests);
    }

    [HttpGet]
    public IActionResult Create()
    {
        // Redirect to wizard
        return RedirectToAction("CreateWizard");
    }

    [HttpGet]
    public IActionResult CreateWizard(string? category = null)
    {
        var dto = new MediAid.DTOs.CreateRequestWizardDto();
        if (!string.IsNullOrEmpty(category))
        {
            dto.Category = category;
        }
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRequestWizardDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _userService.GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            return RedirectToAction("Register", "Account");
        }

        var request = await _requestService.CreateRequestFromWizardAsync(userId, dto);
        if (request == null)
        {
            ModelState.AddModelError("", "Erreur lors de la crÃ©ation de la demande.");
            return View(dto);
        }

        TempData["SuccessMessage"] = "Demande crÃ©Ã©e avec succÃ¨s !";
        return RedirectToAction("Details", new { id = request.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(id);
        
        if (request == null || request.PatientId != userId)
        {
            return NotFound();
        }

        var proposals = await _proposalService.GetProposalsByRequestIdAsync(id);
        var recommendations = await _recommendationService.GetRecommendationsForProposalsAsync(id, proposals);
        ViewBag.Proposals = proposals;
        ViewBag.Recommendations = recommendations;

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _userService.GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            return NotFound();
        }

        var result = await _requestService.CancelRequestAsync(id, userId);
        if (!result)
        {
            TempData["ErrorMessage"] = "Impossible d'annuler cette demande.";
        }
        else
        {
            TempData["SuccessMessage"] = "Demande annulÃ©e avec succÃ¨s.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptProposal(string proposalId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _userService.GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            return NotFound();
        }

        var result = await _proposalService.AcceptProposalAsync(proposalId, userId);
        if (!result)
        {
            TempData["ErrorMessage"] = "Impossible d'accepter cette proposition.";
        }
        else
        {
            TempData["SuccessMessage"] = "Proposition acceptÃ©e !";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectProposal(string proposalId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var patient = await _userService.GetPatientByUserIdAsync(userId);
        if (patient == null)
        {
            return NotFound();
        }

        var result = await _proposalService.RejectProposalAsync(proposalId, userId);
        if (!result)
        {
            TempData["ErrorMessage"] = "Impossible de refuser cette proposition.";
        }
        else
        {
            TempData["SuccessMessage"] = "Proposition refusÃ©e.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockAidant(string aidantId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await _patientService.BlockAidantAsync(userId, aidantId);
        if (!result)
        {
            TempData["ErrorMessage"] = "Impossible de bloquer cet aidant.";
        }
        else
        {
            TempData["SuccessMessage"] = "Aidant bloquÃ© avec succÃ¨s.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var request = await _requestService.GetRequestByIdAsync(id);
        
        if (request == null || request.PatientId != userId)
        {
            TempData["ErrorMessage"] = "Demande introuvable ou vous n'avez pas l'autorisation de la supprimer.";
            return RedirectToAction("Index");
        }

        // VÃ©rifier les contraintes : on ne peut supprimer que les demandes annulÃ©es ou complÃ©tÃ©es
        if (request.Status != "Cancelled" && request.Status != "Completed")
        {
            TempData["ErrorMessage"] = "Vous ne pouvez supprimer que les demandes annulÃ©es ou complÃ©tÃ©es. Les demandes en cours doivent d'abord Ãªtre annulÃ©es.";
            return RedirectToAction("Index");
        }

        var result = await _requestService.DeleteRequestAsync(id, userId);
        if (!result)
        {
            TempData["ErrorMessage"] = "Impossible de supprimer cette demande.";
        }
        else
        {
            TempData["SuccessMessage"] = "Demande supprimÃ©e avec succÃ¨s.";
        }

        return RedirectToAction("Index");
    }
}


