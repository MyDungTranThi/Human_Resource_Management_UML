using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Absences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Controllers;

[Authorize(Roles = AppRoles.HROrAdmin)]
public class AbsencesController : Controller
{
    private readonly HRMDbContext _context;

    public AbsencesController(HRMDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? month, int? year, string? search)
    {
        var now = DateTime.Today;
        var selectedMonth = month ?? now.Month;
        var selectedYear = year ?? now.Year;

        var query = _context.Absences
            .Include(x => x.Emp)
            .Where(x => x.AbsenceDate.Year == selectedYear && x.AbsenceDate.Month == selectedMonth)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Emp.Name.Contains(search) || x.Reason.Contains(search));
        }

        var items = await query.OrderByDescending(x => x.AbsenceDate).ToListAsync();

        var model = new AbsenceIndexViewModel
        {
            Month = selectedMonth,
            Year = selectedYear,
            Search = search,
            Items = items
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AbsenceFormViewModel();
        await PopulateEmployeesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AbsenceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEmployeesAsync(model);
            return View(model);
        }

        var absence = new Absence
        {
            EmpId = model.EmpId,
            AbsenceDate = model.AbsenceDate,
            Reason = model.Reason,
            IsUnpaid = model.IsUnpaid
        };

        _context.Absences.Add(absence);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã thêm ngày nghỉ thành công.";
        return RedirectToAction(nameof(Index), new { month = model.AbsenceDate.Month, year = model.AbsenceDate.Year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Absences.FindAsync(id);
        if (item != null)
        {
            _context.Absences.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa ngày nghỉ.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateEmployeesAsync(AbsenceFormViewModel model)
    {
        model.Employees = await _context.Employees
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.EmpId.ToString(),
                Text = $"{x.Name} (#{x.EmpId})"
            })
            .ToListAsync();
    }
}
