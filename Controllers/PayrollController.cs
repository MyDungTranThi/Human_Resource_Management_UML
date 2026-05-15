using ClosedXML.Excel;
using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Payroll;
using HRM.Web.Services.Payroll;
using HRM.Web.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Controllers;

[Authorize(Roles = AppRoles.AccountingOrAdmin)]
public class PayrollController : Controller
{
    private readonly HRMDbContext _context;
    private readonly PayrollPaymentStore _paymentStore;

    public PayrollController(HRMDbContext context, PayrollPaymentStore paymentStore)
    {
        _context = context;
        _paymentStore = paymentStore;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year, string? search)
    {
        var current = DateTime.Today;
        var selectedMonth = month ?? current.Month;
        var selectedYear = year ?? current.Year;

        var lines = await BuildPayrollLinesAsync(selectedMonth, selectedYear, search);
        var model = new PayrollIndexViewModel
        {
            Month = selectedMonth,
            Year = selectedYear,
            Search = search,
            Lines = lines
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(int month, int year)
    {
        if (month is < 1 or > 12)
        {
            TempData["Error"] = "Month is invalid.";
            return RedirectToAction(nameof(Index));
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var employees = await _context.Employees
            .Include(x => x.Pos)
            .AsNoTracking()
            .ToListAsync();

        var bonusByEmployee = await _context.Rewards
            .Where(x => x.EffectiveDate >= start && x.EffectiveDate < end)
            .GroupBy(x => x.EmpId)
            .Select(g => new { g.Key, Bonus = g.Sum(x => x.Value) })
            .ToDictionaryAsync(x => x.Key, x => x.Bonus);

        var workDaysByEmployee = await _context.Timekeepings
            .Where(x => x.WorkDate >= start && x.WorkDate < end)
            .GroupBy(x => x.EmpId)
            .Select(g => new { g.Key, WorkDays = g.Select(x => x.WorkDate).Distinct().Count() })
            .ToDictionaryAsync(x => x.Key, x => x.WorkDays);

        // Tính tổng số ngày nghỉ từ đầu năm đến trước tháng hiện tại
        var yearStart = new DateOnly(year, 1, 1);
        var absencesBeforeMonth = await _context.Absences
            .Where(x => x.AbsenceDate >= yearStart && x.AbsenceDate < start)
            .GroupBy(x => x.EmpId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        // Lấy số ngày nghỉ trong tháng hiện tại (tất cả các ngày nghỉ, không phân biệt IsUnpaid)
        var absencesInMonth = await _context.Absences
            .Where(x => x.AbsenceDate >= start && x.AbsenceDate < end)
            .GroupBy(x => x.EmpId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var existingSalaryMap = await _context.Salaries
            .Where(x => x.PayrollMonth == month && x.PayrollYear == year)
            .ToDictionaryAsync(x => x.EmpId);

        foreach (var employee in employees)
        {
            var baseSalary = employee.Pos
                .OrderByDescending(x => x.BaseSalary)
                .Select(x => x.BaseSalary)
                .FirstOrDefault();

            // Nếu nhân viên chưa có chức vụ, thử lấy lương mặc định của Staff
            if (baseSalary <= 0)
            {
                var staffPos = await _context.Positions.FirstOrDefaultAsync(p => p.PosName == AppRoles.Staff);
                if (staffPos != null)
                {
                    baseSalary = staffPos.BaseSalary;
                }
            }

            var bonus = bonusByEmployee.TryGetValue(employee.EmpId, out var bonusValue) ? bonusValue : 0;
            
            // Tính số ngày nghỉ không lương tự động
            var leavesBefore = absencesBeforeMonth.TryGetValue(employee.EmpId, out var countBefore) ? countBefore : 0;
            var leavesInMonth = absencesInMonth.TryGetValue(employee.EmpId, out var countInMonth) ? countInMonth : 0;

            // Lấy số ngày phép năm của nhân viên, nếu không có hoặc <= 0 thì mặc định là 12 ngày
            var maxLeaveDaysPerYear = employee.AnnualLeaveDays > 0 ? employee.AnnualLeaveDays : 12m;

            // Số ngày phép còn lại trước tháng này
            var allowedPaidLeavesThisMonth = Math.Max(0m, maxLeaveDaysPerYear - leavesBefore);
            
            // Số ngày nghỉ vượt quá phép (trừ tiền)
            decimal unpaidLeaveDays = Math.Max(0m, leavesInMonth - allowedPaidLeavesThisMonth);

            var perDaySalary = daysInMonth == 0 ? 0m : baseSalary / daysInMonth;
            var deduction = Math.Round(unpaidLeaveDays * perDaySalary, 2);
            var netSalary = Math.Round(baseSalary + bonus - deduction, 2);

            if (existingSalaryMap.TryGetValue(employee.EmpId, out var salary))
            {
                salary.BaseSalary = baseSalary;
                salary.Bonus = bonus;
                salary.Deduction = deduction;
                salary.NetSalary = netSalary;
                salary.LeaveDaysInMonth = unpaidLeaveDays;
            }
            else
            {
                _context.Salaries.Add(new Salary
                {
                    EmpId = employee.EmpId,
                    PayrollMonth = month,
                    PayrollYear = year,
                    BaseSalary = baseSalary,
                    Bonus = bonus,
                    Deduction = deduction,
                    NetSalary = netSalary,
                    LeaveDaysInMonth = unpaidLeaveDays
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Calculated payroll successfully.";

        return RedirectToAction(nameof(Index), new { month, year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmPayment(int empId, int month, int year, string? search)
    {
        _paymentStore.MarkPaid(empId, month, year);
        TempData["Success"] = $"Confirmed payment for employee #{empId}.";

        return RedirectToAction(nameof(Index), new { month, year, search });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int month, int year, string? search)
    {
        var lines = await BuildPayrollLinesAsync(month, year, search);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Payroll");

        sheet.Cell(1, 1).Value = "EmpId";
        sheet.Cell(1, 2).Value = "EmployeeName";
        sheet.Cell(1, 3).Value = "Department";
        sheet.Cell(1, 4).Value = "BaseSalary";
        sheet.Cell(1, 5).Value = "Bonus";
        sheet.Cell(1, 6).Value = "Deduction";
        sheet.Cell(1, 7).Value = "NetSalary";
        sheet.Cell(1, 8).Value = "UnpaidLeaveDays";
        sheet.Cell(1, 9).Value = "PaidStatus";

        for (var i = 0; i < lines.Count; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = lines[i].EmpId;
            sheet.Cell(row, 2).Value = lines[i].EmployeeName;
            sheet.Cell(row, 3).Value = lines[i].DepartmentName;
            sheet.Cell(row, 4).Value = lines[i].BaseSalary;
            sheet.Cell(row, 5).Value = lines[i].Bonus;
            sheet.Cell(row, 6).Value = lines[i].Deduction;
            sheet.Cell(row, 7).Value = lines[i].NetSalary;
            sheet.Cell(row, 8).Value = lines[i].LeaveDaysInMonth;
            sheet.Cell(row, 9).Value = lines[i].IsPaid ? "Paid" : "Unpaid";
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"payroll_{year}_{month:00}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(int month, int year, string? search)
    {
        var lines = await BuildPayrollLinesAsync(month, year, search);

        var reportLines = lines.Select(x =>
            $"#{x.EmpId} | {x.EmployeeName} | Net: {x.NetSalary:N0} | Leave: {x.LeaveDaysInMonth} | {(x.IsPaid ? "Paid" : "Unpaid")}");

        var bytes = SimplePdfExporter.CreatePdf($"Payroll Report {year}-{month:00}", reportLines);
        return File(bytes, "application/pdf", $"payroll_{year}_{month:00}.pdf");
    }

    private async Task<IReadOnlyList<PayrollLineViewModel>> BuildPayrollLinesAsync(int month, int year, string? search)
    {
        var query = _context.Salaries
            .Include(x => x.Emp)
                .ThenInclude(x => x.Dept)
            .Where(x => x.PayrollMonth == month && x.PayrollYear == year)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.Emp.Name.Contains(keyword) ||
                x.Emp.Email.Contains(keyword) ||
                x.Emp.Dept.DeptName.Contains(keyword));
        }

        var rows = await query
            .OrderBy(x => x.EmpId)
            .Select(x => new PayrollLineViewModel
            {
                EmpId = x.EmpId,
                EmployeeName = x.Emp.Name,
                DepartmentName = x.Emp.Dept.DeptName,
                BaseSalary = x.BaseSalary,
                Bonus = x.Bonus,
                Deduction = x.Deduction,
                NetSalary = x.NetSalary,
                LeaveDaysInMonth = x.LeaveDaysInMonth,
                IsPaid = _paymentStore.IsPaid(x.EmpId, month, year)
            })
            .ToListAsync();

        return rows;
    }
}
