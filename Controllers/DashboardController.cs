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

        var employeeCount = await _context.Employees.CountAsync();

        var model = new DashboardViewModel
        {
            EmployeeCount = employeeCount,
            DepartmentCount = await _context.Departments.CountAsync(),
            ProjectCount = await _context.Projects.CountAsync(),
            ActiveProjectCount = await _context.Projects.CountAsync(p => p.ProjStatus == "On Going"),
            ThisMonthPayrollTotal = await _context.Salaries
                .Where(x => x.PayrollMonth == month && x.PayrollYear == year)
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0,
            CurrentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty
        };

        // 1. Xu hướng lương (6 tháng gần nhất)
        var sixMonthsAgo = today.AddMonths(-5);
        model.SalaryTrend = await _context.Salaries
            .Where(s => (s.PayrollYear > sixMonthsAgo.Year) || (s.PayrollYear == sixMonthsAgo.Year && s.PayrollMonth >= sixMonthsAgo.Month))
            .GroupBy(s => new { s.PayrollYear, s.PayrollMonth })
            .Select(g => new MonthlySalaryStat
            {
                Year = g.Key.PayrollYear,
                Month = g.Key.PayrollMonth,
                Total = g.Sum(x => x.NetSalary)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        // 2. Phân bổ nhân sự theo phòng ban
        if (employeeCount > 0)
        {
            model.DeptDistribution = await _context.Employees
                .Include(e => e.Dept)
                .GroupBy(e => e.Dept.DeptName)
                .Select(g => new DepartmentStat
                {
                    DeptName = g.Key ?? "Chưa phân loại",
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / employeeCount * 100, 1)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();
        }

        // 3. Hoạt động gần đây (lấy các bản ghi nghỉ phép mới nhất)
        model.Activities = await _context.Absences
            .Include(a => a.Emp).ThenInclude(e => e.Dept)
            .OrderByDescending(a => a.AbsenceDate)
            .Take(5)
            .Select(a => new RecentActivityViewModel
            {
                EmployeeName = a.Emp.Name,
                DeptName = a.Emp.Dept.DeptName,
                Action = "Đăng ký nghỉ phép",
                DateLabel = a.AbsenceDate.ToString("dd/MM/yyyy"),
                Status = "Ghi nhận",
                StatusClass = "active"
            })
            .ToListAsync();

        return View(model);
    }
}
