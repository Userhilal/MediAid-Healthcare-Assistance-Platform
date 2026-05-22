using MediAid.DTOs;
using MediAid.Models;
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
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(dto);
        }

        var user = await _authService.LoginAsync(dto.Email, dto.Password);

        if (user == null)
        {
            ModelState.AddModelError("", "Email ou mot de passe incorrect, ou compte désactivé.");
            ViewBag.ReturnUrl = returnUrl;
            return View(dto);
        }

        await SignInUserAsync(user);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        if (dto.Role != "Patient" && dto.Role != "Aidant")
        {
            ModelState.AddModelError("", "Le rôle sélectionné n'est pas autorisé à l'inscription publique.");
            return View(dto);
        }

        var success = await _authService.RegisterAsync(
            dto.Email,
            dto.Password,
            dto.FirstName ?? "",
            dto.LastName ?? "",
            dto.PhoneNumber,
            dto.Role
        );

        if (!success)
        {
            ModelState.AddModelError("", "Inscription impossible. Vérifiez vos informations ou utilisez un autre email.");
            return View(dto);
        }

        TempData["SuccessMessage"] = "Inscription réussie. Vous pouvez maintenant vous connecter.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
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

        var user = await GetCurrentUserAsync();

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
        {
            ModelState.AddModelError("", "Ancien mot de passe incorrect.");
            return View(dto);
        }

        var success = await _authService.ChangePasswordAsync(user.Id!, dto.NewPassword);

        if (!success)
        {
            ModelState.AddModelError("", "Erreur lors du changement de mot de passe.");
            return View(dto);
        }

        TempData["SuccessMessage"] = "Mot de passe changé avec succès.";
        return RedirectToAction("Index", "Profile");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChangeEmail()
    {
        var user = await GetCurrentUserAsync();

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ChangeEmailDto { CurrentEmail = user.Email });
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

        var user = await GetCurrentUserAsync();

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Mot de passe incorrect.");
            dto.CurrentEmail = user.Email;
            return View(dto);
        }

        var normalizedEmail = dto.NewEmail.Trim().ToLowerInvariant();

        var existingUser = await _userService.GetUserByEmailAsync(normalizedEmail);

        if (existingUser != null && existingUser.Id != user.Id)
        {
            ModelState.AddModelError("", "Cet email est déjà utilisé.");
            dto.CurrentEmail = user.Email;
            return View(dto);
        }

        user.Email = normalizedEmail;
        user.UpdatedAt = DateTime.UtcNow;

        var success = await _userService.UpdateUserAsync(user);

        if (!success)
        {
            ModelState.AddModelError("", "Erreur lors du changement d'email.");
            dto.CurrentEmail = user.Email;
            return View(dto);
        }

        await SignInUserAsync(user);

        TempData["SuccessMessage"] = "Email changé avec succès.";
        return RedirectToAction("Index", "Profile");
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
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["SuccessMessage"] = "Déconnexion effectuée.";
        return RedirectToAction(nameof(Login));
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

        var user = await GetCurrentUserAsync();

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!dto.ConfirmDeletion)
        {
            ModelState.AddModelError("", "Vous devez confirmer la suppression du compte.");
            return View(dto);
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Mot de passe incorrect.");
            return View(dto);
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        var success = await _userService.UpdateUserAsync(user);

        if (!success)
        {
            ModelState.AddModelError("", "Erreur lors de la suppression du compte.");
            return View(dto);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["SuccessMessage"] = "Votre compte a été désactivé avec succès.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _userService.GetUserByIdAsync(userId);
    }

    private async Task SignInUserAsync(MediAid.Models.User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            claims.Add(new Claim("FirstName", user.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            claims.Add(new Claim("LastName", user.LastName));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );
    }
}


