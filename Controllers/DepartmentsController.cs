using System;
using ClosedXML.Excel;
using HRM.Web.Helpers;
using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Departments;
using HRM.Web.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Controllers;

[Authorize(Roles = AppRoles.ManagerOrAdmin)]
public class DepartmentsController : Controller
{
    private readonly HRMDbContext _context;

    public DepartmentsController(HRMDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 5 or > 100 ? 10 : pageSize;

        var query = _context.Departments
            .Include(x => x.DeptManager)
            .Include(x => x.Employees)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.DeptName.Contains(keyword) ||
                x.DeptManager.Name.Contains(keyword));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.DeptId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new DepartmentIndexViewModel
        {
            Search = search,
            PageSize = pageSize,
            PagedDepartments = new PagedResult<Department>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormAsync(new DepartmentFormViewModel());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var department = new Department
        {
            DeptName = model.DeptName.Trim(),
            DeptManagerId = model.DeptManagerId
        };

        _context.Departments.Add(department);

        try
        {
            await _context.SaveChangesAsync();
            
            // TỰ ĐỘNG: Chuyển Trưởng phòng về đúng phòng ban mà họ quản lý
            var manager = await _context.Employees.FindAsync(model.DeptManagerId);
            if (manager != null)
            {
                manager.DeptId = department.DeptId;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đã thêm phòng ban mới và tự động cập nhật phòng ban cho Trưởng phòng.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi khi tạo phòng ban: {ex.Message}");
            return View(await BuildFormAsync(model));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(x => x.DeptId == id);
        if (department == null)
        {
            return NotFound();
        }

        var model = new DepartmentFormViewModel
        {
            DeptId = department.DeptId,
            DeptName = department.DeptName,
            DeptManagerId = department.DeptManagerId
        };

        model = await BuildFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentFormViewModel model)
    {
        if (model.DeptId != id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var department = await _context.Departments
            .Include(x => x.Employees)
            .FirstOrDefaultAsync(x => x.DeptId == id);
        if (department == null)
        {
            return NotFound();
        }

        var oldManagerId = department.DeptManagerId;
        department.DeptName = model.DeptName.Trim();
        department.DeptManagerId = model.DeptManagerId;

        if (oldManagerId != model.DeptManagerId)
        {
            foreach (var emp in department.Employees)
            {
                // ManagerId removed
            }
        }

        try
        {
            await _context.SaveChangesAsync();

            // TỰ ĐỘNG: Chuyển Trưởng phòng mới về đúng phòng ban mà họ quản lý
            var manager = await _context.Employees.FindAsync(model.DeptManagerId);
            if (manager != null)
            {
                manager.DeptId = id;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đã cập nhật thông tin phòng ban và Trưởng phòng.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi khi cập nhật: {ex.Message}");
            return View(await BuildFormAsync(model));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(x => x.DeptId == id);
        if (department == null)
        {
            TempData["Error"] = "Department not found.";
            return RedirectToAction(nameof(Index));
        }

        _context.Departments.Remove(department);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Deleted department successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Could not delete department due to related employees/projects.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Members(int id)
    {
        var department = await _context.Departments
            .Include(x => x.Employees)
            .FirstOrDefaultAsync(x => x.DeptId == id);

        if (department == null)
        {
            return NotFound();
        }

        var availableEmployees = await _context.Employees
            .Where(x => x.DeptId != id)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var model = new DepartmentMembersViewModel
        {
            DeptId = department.DeptId,
            DeptName = department.DeptName,
            CurrentEmployees = department.Employees.OrderBy(x => x.Name).ToList(),
            AvailableEmployees = availableEmployees
        };

        ViewBag.TransferDepartments = await _context.Departments
            .Where(x => x.DeptId != id)
            .OrderBy(x => x.DeptName)
            .Select(x => new SelectListItem
            {
                Value = x.DeptId.ToString(),
                Text = x.DeptName
            })
            .ToListAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEmployees(int id, List<int>? employeeIds)
    {
        if (employeeIds == null || employeeIds.Count == 0)
        {
            TempData["Error"] = "Please choose employees to add.";
            return RedirectToAction(nameof(Members), new { id });
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(x => x.DeptId == id);
        if (department == null)
        {
            TempData["Error"] = "Department not found.";
            return RedirectToAction(nameof(Members), new { id });
        }

        var employees = await _context.Employees
            .Where(x => employeeIds.Contains(x.EmpId))
            .ToListAsync();

        foreach (var employee in employees)
        {
            employee.DeptId = id;
            // ManagerId removed
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Assigned employees to department.";
        return RedirectToAction(nameof(Members), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferEmployee(int id, int employeeId, int targetDeptId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.EmpId == employeeId && x.DeptId == id);
        if (employee == null)
        {
            TempData["Error"] = "Employee not found in this department.";
            return RedirectToAction(nameof(Members), new { id });
        }

        // Lấy DeptManager của phòng đích để cập nhật ManagerId
        var targetDept = await _context.Departments.FirstOrDefaultAsync(x => x.DeptId == targetDeptId);
        if (targetDept == null)
        {
            TempData["Error"] = "Target department not found.";
            return RedirectToAction(nameof(Members), new { id });
        }

        employee.DeptId = targetDeptId;
        // ManagerId removed
        await _context.SaveChangesAsync();

        TempData["Success"] = "Transferred employee successfully.";
        return RedirectToAction(nameof(Members), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var data = await _context.Departments
            .Include(x => x.DeptManager)
            .Include(x => x.Employees)
            .OrderBy(x => x.DeptId)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Departments");

        sheet.Cell(1, 1).Value = "DeptId";
        sheet.Cell(1, 2).Value = "DeptName";
        sheet.Cell(1, 3).Value = "Manager";
        sheet.Cell(1, 4).Value = "EmployeeCount";

        for (var i = 0; i < data.Count; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = data[i].DeptId;
            sheet.Cell(row, 2).Value = data[i].DeptName;
            sheet.Cell(row, 3).Value = data[i].DeptManager.Name;
            sheet.Cell(row, 4).Value = data[i].Employees.Count;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"departments_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var data = await _context.Departments
            .Include(x => x.DeptManager)
            .Include(x => x.Employees)
            .OrderBy(x => x.DeptId)
            .Take(100)
            .ToListAsync();

        var lines = data.Select(x =>
            $"#{x.DeptId} | {x.DeptName} | Manager: {x.DeptManager.Name} | Members: {x.Employees.Count}");

        var bytes = SimplePdfExporter.CreatePdf("Department Report", lines);
        return File(bytes, "application/pdf", $"departments_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    private async Task<DepartmentFormViewModel> BuildFormAsync(DepartmentFormViewModel model)
    {
        // 1. Lấy danh sách ID của những người đang làm Trưởng phòng ở các phòng khác
        var otherManagers = await _context.Departments
            .Where(d => d.DeptId != model.DeptId)
            .Select(d => d.DeptManagerId)
            .Where(id => id != null)
            .ToListAsync();

        // 2. Lấy nhân viên kèm Phòng ban và Chức vụ, lọc theo phòng ban
        var employeesQuery = _context.Employees
            .Include(x => x.Dept)
            .Include(x => x.Pos)
            .Where(x => !otherManagers.Contains(x.EmpId));

        if (model.DeptId.HasValue && model.DeptId.Value > 0)
        {
            // Nếu đang Edit: chỉ chọn nhân viên đang ở phòng này
            employeesQuery = employeesQuery.Where(x => x.DeptId == model.DeptId.Value);
        }

        var employees = await employeesQuery
            .OrderBy(x => x.Name)
            .ToListAsync();

        // Lọc in-memory để tìm các nhân viên có chức vụ là Dept head / Trưởng phòng cho cả trường hợp Create và Edit
        employees = employees.Where(x => x.Pos.Any(p => 
            p.PosName.Contains("head", StringComparison.OrdinalIgnoreCase) || 
            p.PosName.Contains("Trưởng", StringComparison.OrdinalIgnoreCase) ||
            p.PosName.Contains("Dept", StringComparison.OrdinalIgnoreCase))).ToList();

        // 3. Tạo danh sách chọn với đầy đủ thông tin để chẩn đoán
        model.Managers = employees.Select(x => new SelectListItem
        {
            Value = x.EmpId.ToString(),
            Text = $"{x.Name} (#{x.EmpId}) - [Phòng: {x.Dept?.DeptName ?? "Chưa có"}] - [Chức vụ: {(x.Pos.Any() ? string.Join(", ", x.Pos.Select(p => p.PosName)) : "Chưa gán")}]",
            Selected = model.DeptManagerId == x.EmpId
        }).ToList();

        return model;
    }
}
