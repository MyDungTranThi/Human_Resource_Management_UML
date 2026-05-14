using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var isHr = User.IsInRole(AppRoles.HR) || isAdmin;
        var isAccountant = User.IsInRole(AppRoles.Accountant) || isAdmin;
        var isManager = User.IsInRole(AppRoles.Manager) || isAdmin;

        var today = DateTime.Today;
        var thisMonth = today.Month;
        var thisYear = today.Year;

        var model = new ReportsDashboardViewModel
        {
            ShowHrMetrics = isHr,
            ShowAccountantMetrics = isAccountant,
            ShowManagerMetrics = isManager
        };

        // --- HR Metrics ---
        if (isHr)
        {
            model.TotalHeadcount = await _context.Employees.CountAsync();
            
            var mostAbsent = await _context.Absences
                .GroupBy(a => a.EmpId)
                .Select(g => new { EmpId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();
                
            if (mostAbsent != null)
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmpId == mostAbsent.EmpId);
                model.MostAbsentEmployeeName = emp?.Name ?? "Không xác định";
                model.MostAbsentCount = mostAbsent.Count;
            }

            model.HrEmployeeList = await _context.Employees
                .Include(e => e.Dept)
                .Include(e => e.Pos)
                .OrderBy(e => e.Name)
                .Select(e => new HrEmployeeReportItem
                {
                    EmpId = e.EmpId,
                    Name = e.Name,
                    Department = e.Dept != null ? e.Dept.DeptName : "N/A",
                    Position = string.Join(", ", e.Pos.Select(p => p.PosName)),
                    Status = e.Status
                })
                .ToListAsync();

            model.HrAbsenceList = await _context.Absences
                .Include(a => a.Emp)
                .OrderByDescending(a => a.AbsenceDate)
                .Take(50) // Giới hạn 50 lượt nghỉ gần nhất
                .Select(a => new HrAbsenceReportItem
                {
                    EmployeeName = a.Emp.Name,
                    Date = a.AbsenceDate.ToString("dd/MM/yyyy"),
                    Reason = a.Reason ?? "Không có lý do"
                })
                .ToListAsync();
        }

        // --- Accountant Metrics ---
        if (isAccountant)
        {
            model.PayrollCost = await _context.Salaries
                .Where(x => x.PayrollMonth == thisMonth && x.PayrollYear == thisYear)
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0;

            model.SalaryRanking = await _context.Salaries
                .Where(x => x.PayrollMonth == thisMonth && x.PayrollYear == thisYear)
                .Include(x => x.Emp).ThenInclude(e => e.Dept)
                .OrderByDescending(x => x.NetSalary)
                .Select(x => new SalaryRankingItem
                {
                    EmployeeName = x.Emp.Name,
                    DepartmentName = x.Emp.Dept != null ? x.Emp.Dept.DeptName : "N/A",
                    Salary = x.NetSalary
                })
                .ToListAsync();
        }

        // --- Manager Metrics ---
        if (isManager)
        {
            model.TotalDepartments = await _context.Departments.CountAsync();
            model.TotalProjects = await _context.Projects.CountAsync();

            model.ManagerDepartmentList = await _context.Departments
                .Include(d => d.DeptManager)
                .Include(d => d.Employees)
                .OrderBy(d => d.DeptName)
                .Select(d => new ManagerDepartmentItem
                {
                    DeptName = d.DeptName,
                    ManagerName = d.DeptManager != null ? d.DeptManager.Name : "Chưa có",
                    EmployeeCount = d.Employees.Count
                })
                .ToListAsync();

            model.ManagerProjectList = await _context.Projects
                .OrderBy(p => p.ProjName)
                .Select(p => new ManagerProjectItem
                {
                    ProjName = p.ProjName,
                    Status = p.ProjStatus,
                    Budget = (decimal)p.Budget
                })
                .ToListAsync();
        }

        // --- Admin/Shared Metrics (Charts) ---
        if (isAdmin)
        {
            var sixMonths = Enumerable.Range(0, 6)
                .Select(offset => today.AddMonths(-(5 - offset)))
                .ToList();

            var salaryData = await _context.Salaries
                .Where(x => sixMonths.Select(m => m.Year).Contains(x.PayrollYear))
                .ToListAsync();

            model.MonthlyPayrollTrend = sixMonths.Select(monthDate =>
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

            model.DepartmentRetention = await _context.Employees
                .Include(x => x.Dept)
                .Where(x => x.Dept != null)
                .GroupBy(x => x.Dept!.DeptName)
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
                
            model.ActiveProjects = await _context.Projects.CountAsync(x => x.ProjStatus == "Active" || x.ProjStatus == "In Progress");
            
            var startDates = await _context.EmploymentContracts.Select(x => x.StartDate).ToListAsync();
            var avgTenureYears = startDates.Count == 0 ? 0m : (decimal)startDates.Average(x => (today - x.ToDateTime(TimeOnly.MinValue).Date).TotalDays) / 365m;
            model.AverageTenureYears = Math.Round(avgTenureYears, 1);
        }

        return View(model);
    }
}
