using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.HR},{AppRoles.Manager},{AppRoles.Accountant}")]
public class ReportsController : Controller
{
    private readonly HRMDbContext _context;

    public ReportsController(HRMDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var thisMonth = today.Month;
        var thisYear = today.Year;

        var sixMonths = Enumerable.Range(0, 6)
            .Select(offset => today.AddMonths(-(5 - offset)))
            .ToList();

        var salaryData = await _context.Salaries
            .Where(x => sixMonths.Select(m => m.Year).Contains(x.PayrollYear))
            .ToListAsync();

        var trend = sixMonths.Select(monthDate =>
        {
            var monthPayroll = salaryData
                .Where(x => x.PayrollYear == monthDate.Year && x.PayrollMonth == monthDate.Month)
                .Sum(x => x.NetSalary);

            return new MonthlyPayrollPoint
            {
                Label = monthDate.ToString("MMM yyyy"),
                Payroll = monthPayroll,
                Budget = monthPayroll * 1.08m
            };
        }).ToList();

        var retention = await _context.Employees
            .Include(x => x.Dept)
            .GroupBy(x => x.Dept.DeptName)
            .Select(g => new DepartmentRetentionItem
            {
                DepartmentName = g.Key,
                RetentionPercent = g.Count() == 0
                    ? 0
                    : Math.Round(100m * g.Count(x => x.Status == "Active") / g.Count(), 0)
            })
            .OrderByDescending(x => x.RetentionPercent)
            .Take(5)
            .ToListAsync();

        var startDates = await _context.EmploymentContracts
            .Select(x => x.StartDate)
            .ToListAsync();

        var avgTenureYears = startDates.Count == 0
            ? 0m
            : (decimal)startDates.Average(x =>
                (today - x.ToDateTime(TimeOnly.MinValue).Date).TotalDays) / 365m;

        var model = new ReportsDashboardViewModel
        {
            TotalHeadcount = await _context.Employees.CountAsync(),
            PayrollCost = await _context.Salaries
                .Where(x => x.PayrollMonth == thisMonth && x.PayrollYear == thisYear)
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0,
            ActiveProjects = await _context.Projects.CountAsync(x => x.ProjStatus == "Active" || x.ProjStatus == "In Progress"),
            AverageTenureYears = Math.Round(avgTenureYears, 1),
            MonthlyPayrollTrend = trend,
            DepartmentRetention = retention
        };

        return View(model);
    }
}
