using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Auth;
using HRM.Web.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.Web.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    private readonly HRMDbContext _context;

    public AuthController(HRMDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _context.Accounts.FirstOrDefault(x => x.Username == model.Username);

        if (user == null)
        {
            ViewBag.Error = "Sai tai khoan hoac mat khau.";
            return View(model);
        }

        if (user.IsActive == false)
        {
            ViewBag.Error = "Tai khoan dang bi khoa.";
            return View(model);
        }

        if (!PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            ViewBag.Error = "Sai tai khoan hoac mat khau.";
            return View(model);
        }

        var normalizedRole = NormalizeRole(user.Role);

        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.AccountId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, normalizedRole)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return normalizedRole switch
        {
            AppRoles.HR => RedirectToAction(nameof(EmployeesController.Index), "Employees"),
            AppRoles.Accountant => RedirectToAction(nameof(PayrollController.Index), "Payroll"),
            AppRoles.Manager => RedirectToAction(nameof(ProjectsController.Index), "Projects"),
            _ => RedirectToAction(nameof(DashboardController.Index), "Dashboard")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static string NormalizeRole(string? role)
    {
        var safeRole = role?.Trim() ?? string.Empty;
        if (safeRole.Length == 0)
        {
            return safeRole;
        }

        return safeRole.ToLowerInvariant() switch
        {
            "admin" => AppRoles.Admin,
            "administrator" => AppRoles.Admin,
            "hr" => AppRoles.HR,
            "humanresource" => AppRoles.HR,
            "humanresources" => AppRoles.HR,
            "accounting" => AppRoles.Accountant,
            "accountant" => AppRoles.Accountant,
            "finance" => AppRoles.Accountant,
            "manager" => AppRoles.Manager,
            "management" => AppRoles.Manager,
            _ => safeRole
        };
    }
}
