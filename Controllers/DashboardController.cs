using HRM.Web.Models.ViewModels.Dashboard;
using HRM.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using HRM.Web.Models.Constants;

namespace HRM.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly HRMDbContext _context;

    public DashboardController(HRMDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var month = today.Month;
        var year = today.Year;

        var model = new DashboardViewModel
        {
            EmployeeCount = await _context.Employees.CountAsync(),
            DepartmentCount = await _context.Departments.CountAsync(),
            ProjectCount = await _context.Projects.CountAsync(),
            ActiveProjectCount = await _context.Projects.CountAsync(p => p.ProjStatus == "Active" || p.ProjStatus == "In Progress"),
            ThisMonthPayrollTotal = await _context.Salaries
                .Where(x => x.PayrollMonth == month && x.PayrollYear == year)
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0,
            CurrentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty
        };

        return View(model);
    }
}
