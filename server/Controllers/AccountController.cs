using MediAid.DTOs;
using MediAid.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediAid.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AccountController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var user = await _authService.LoginAsync(dto.Email, dto.Password);
        if (user == null)
        {
            ModelState.AddModelError("", "Email ou mot de passe incorrect.");
            return View(dto);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var success = await _authService.RegisterAsync(dto.Email, dto.Password, dto.FirstName ?? "", dto.LastName ?? "", dto.PhoneNumber, dto.Role);
        if (!success)
        {
            ModelState.AddModelError("", "Erreur lors de l'inscription. L'email existe peut-être déjà.");
            return View(dto);
        }

        TempData["SuccessMessage"] = "Inscription réussie ! Vous pouvez maintenant vous connecter.";
        return RedirectToAction("Login");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Verify old password
        var loginResult = await _authService.LoginAsync(user.Email, dto.OldPassword);
        if (loginResult == null)
        {
            ModelState.AddModelError("", "Ancien mot de passe incorrect.");
            return View(dto);
        }

        // Update password
        var success = await _authService.ChangePasswordAsync(userId, dto.NewPassword);
        if (!success)
        {
            ModelState.AddModelError("", "Erreur lors du changement de mot de passe.");
            return View(dto);
        }

        TempData["SuccessMessage"] = "Mot de passe changé avec succès.";
        return RedirectToAction("Profile", "Patient");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChangeEmail()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var dto = new ChangeEmailDto { CurrentEmail = user.Email };
        return View(dto);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(ChangeEmailDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Verify password
        var loginResult = await _authService.LoginAsync(user.Email, dto.Password);
        if (loginResult == null)
        {
            ModelState.AddModelError("", "Mot de passe incorrect.");
            return View(dto);
        }

        // Check if new email already exists
        var existingUser = await _userService.GetUserByEmailAsync(dto.NewEmail);
        if (existingUser != null && existingUser.Id != userId)
        {
            ModelState.AddModelError("", "Cet email est déjà utilisé.");
            return View(dto);
        }

        // Update email
        user.Email = dto.NewEmail;
        user.UpdatedAt = DateTime.UtcNow;
        var success = await _userService.UpdateUserAsync(user);
        if (!success)
        {
            ModelState.AddModelError("", "Erreur lors du changement d'email.");
            return View(dto);
        }

        // Update claims and re-sign in
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        TempData["SuccessMessage"] = "Email changé avec succès.";
        return RedirectToAction("Profile", "Patient");
    }

    [Authorize]
    [HttpGet]
    public IActionResult LogoutAll()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("LogoutAll")]
    public async Task<IActionResult> LogoutAllPost()
    {
        // In a real application, you would invalidate all refresh tokens or session tokens
        // For now, we'll just sign out the current session
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        TempData["SuccessMessage"] = "Déconnexion effectuée sur tous les appareils.";
        return RedirectToAction("Login");
    }

    [Authorize]
    [HttpGet]
    public IActionResult DeleteAccount()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Verify password
        var loginResult = await _authService.LoginAsync(user.Email, dto.Password);
        if (loginResult == null)
        {
            ModelState.AddModelError("", "Mot de passe incorrect.");
            return View(dto);
        }

        // Delete account (soft delete - mark as deleted)
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        var success = await _userService.UpdateUserAsync(user);
        if (!success)
        {
            ModelState.AddModelError("", "Erreur lors de la suppression du compte.");
            return View(dto);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Votre compte a été supprimé avec succès.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
}

