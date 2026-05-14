using ClosedXML.Excel;
using HRM.Web.Helpers;
using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Employees;
using HRM.Web.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Controllers;

[Authorize(Roles = AppRoles.HROrAdmin)]
public class EmployeesController : Controller
{
    private readonly HRMDbContext _context;

    public EmployeesController(HRMDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 5 or > 100 ? 10 : pageSize;

        var query = _context.Employees
            .Include(x => x.Dept)
            .Include(x => x.Pos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Email.Contains(keyword) ||
                x.PhoneNumber.Contains(keyword) ||
                x.Status.Contains(keyword));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.EmpId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var stats = await _context.Employees
            .Include(x => x.Dept)
            .GroupBy(x => x.Dept.DeptName)
            .Select(g => new EmployeeDepartmentStatViewModel
            {
                DepartmentName = g.Key,
                EmployeeCount = g.Count()
            })
            .OrderByDescending(x => x.EmployeeCount)
            .ToListAsync();

        var model = new EmployeeIndexViewModel
        {
            Search = search,
            PageSize = pageSize,
            PagedEmployees = new PagedResult<Employee>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            },
            DepartmentStats = stats
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EmployeeFormViewModel
        {
            CanEditPosition = User.IsInRole(AppRoles.Admin)
        };
        model = await BuildEmployeeFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildEmployeeFormAsync(model));
        }

        var allowedStatuses = new[] { "Working", "On leave", "Resigned" };
        if (string.IsNullOrWhiteSpace(model.Status) || !allowedStatuses.Contains(model.Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Status), "Trạng thái không hợp lệ.");
        }

        var emailExists = await _context.Employees.AnyAsync(x => x.Email == model.Email.Trim());
        var phoneExists = await _context.Employees.AnyAsync(x => x.PhoneNumber == model.PhoneNumber.Trim());

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
        }

        if (phoneExists)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEmployeeFormAsync(model));
        }

        // ManagerId removed from Employee table. Hierarchy is now dynamic via Dept.DeptManagerId.

        // Chuẩn bị thông tin nhân viên
        var employee = new Employee
        {
            Name = model.Name.Trim(),
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            Address = model.Address.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = model.Email.Trim(),
            DeptId = model.DeptId,
            AnnualLeaveDays = model.AnnualLeaveDays,
            Status = model.Status.Trim(),
            ImagePath = string.IsNullOrWhiteSpace(model.ImagePath) ? null : model.ImagePath.Trim()
        };

        // Tìm chức vụ để gán
        Position? targetPosition = null;
        if ((User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager)) && model.PositionId.HasValue && model.PositionId > 0)
        {
            targetPosition = await _context.Positions.FindAsync(model.PositionId.Value);
        }

        // Nếu không chọn hoặc không có quyền, mặc định là Staff
        if (targetPosition == null)
        {
            targetPosition = await _context.Positions.FirstOrDefaultAsync(p => p.PosName == AppRoles.Staff);
        }

        if (targetPosition != null)
        {
            employee.Pos.Add(targetPosition);
        }

        _context.Employees.Add(employee);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm nhân viên mới thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(string.Empty, "Could not save employee. Please check unique fields (email/phone)." );
            if (ex.InnerException != null)
            {
                ModelState.AddModelError(string.Empty, ex.InnerException.Message);
            }
            return View(await BuildEmployeeFormAsync(model));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.Employees
            .Include(x => x.Pos)
            .FirstOrDefaultAsync(x => x.EmpId == id);
        if (employee == null)
        {
            return NotFound();
        }

        var model = new EmployeeFormViewModel
        {
            EmpId = employee.EmpId,
            Name = employee.Name,
            DateOfBirth = employee.DateOfBirth,
            Gender = employee.Gender,
            Address = employee.Address,
            PhoneNumber = employee.PhoneNumber,
            Email = employee.Email,
            DeptId = employee.DeptId,
            PositionId = employee.Pos.FirstOrDefault()?.PosId,
            PositionName = employee.Pos.FirstOrDefault()?.PosName,
            CanEditPosition = User.IsInRole(AppRoles.Admin),
            AnnualLeaveDays = employee.AnnualLeaveDays,
            Status = employee.Status,
            ImagePath = employee.ImagePath
        };

        model = await BuildEmployeeFormAsync(model, employee.EmpId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        if (model.EmpId != id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEmployeeFormAsync(model, id));
        }

        var allowedStatuses = new[] { "Working", "On leave", "Resigned" };
        if (string.IsNullOrWhiteSpace(model.Status) || !allowedStatuses.Contains(model.Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Status), "Trạng thái không hợp lệ.");
        }

        var emailExists = await _context.Employees.AnyAsync(x => x.EmpId != id && x.Email == model.Email.Trim());
        var phoneExists = await _context.Employees.AnyAsync(x => x.EmpId != id && x.PhoneNumber == model.PhoneNumber.Trim());

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
        }

        if (phoneExists)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEmployeeFormAsync(model, id));
        }

        var employee = await _context.Employees
            .Include(x => x.Pos)
            .FirstOrDefaultAsync(x => x.EmpId == id);
        if (employee == null)
        {
            return NotFound();
        }

        employee.Name = model.Name.Trim();
        employee.DateOfBirth = model.DateOfBirth;
        employee.Gender = model.Gender;
        employee.Address = model.Address.Trim();
        employee.PhoneNumber = model.PhoneNumber.Trim();
        employee.Email = model.Email.Trim();
        employee.AnnualLeaveDays = model.AnnualLeaveDays;
        employee.Status = model.Status.Trim();
        employee.ImagePath = string.IsNullOrWhiteSpace(model.ImagePath) ? null : model.ImagePath.Trim();

        // ManagerId removed from Employee table.

        employee.DeptId = model.DeptId;

        // Cho phép cả Admin và Manager cập nhật chức vụ
        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager))
        {
            employee.Pos.Clear();
            if (model.PositionId.HasValue && model.PositionId > 0)
            {
                var position = await _context.Positions.FindAsync(model.PositionId.Value);
                if (position != null)
                {
                    employee.Pos.Add(position);
                }
            }
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Updated employee successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(string.Empty, "Could not update employee. Please check unique fields (email/phone)." );
            if (ex.InnerException != null)
            {
                ModelState.AddModelError(string.Empty, ex.InnerException.Message);
            }
            return View(await BuildEmployeeFormAsync(model, id));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.EmpId == id);
        if (employee == null)
        {
            TempData["Error"] = "Employee not found.";
            return RedirectToAction(nameof(Index));
        }

        _context.Employees.Remove(employee);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Deleted employee successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Could not delete employee because of related data.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? search)
    {
        var data = await BuildFilteredQuery(search)
            .OrderBy(x => x.EmpId)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Employees");

        sheet.Cell(1, 1).Value = "EmpId";
        sheet.Cell(1, 2).Value = "Name";
        sheet.Cell(1, 3).Value = "Email";
        sheet.Cell(1, 4).Value = "Phone";
        sheet.Cell(1, 5).Value = "Department";
        sheet.Cell(1, 6).Value = "Status";

        for (var i = 0; i < data.Count; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = data[i].EmpId;
            sheet.Cell(row, 2).Value = data[i].Name;
            sheet.Cell(row, 3).Value = data[i].Email;
            sheet.Cell(row, 4).Value = data[i].PhoneNumber;
            sheet.Cell(row, 5).Value = data[i].Dept.DeptName;
            sheet.Cell(row, 6).Value = data[i].Status;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"employees_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? search)
    {
        var data = await BuildFilteredQuery(search)
            .OrderBy(x => x.EmpId)
            .Take(100)
            .ToListAsync();

        var lines = data.Select(x =>
            $"#{x.EmpId} | {x.Name} | {x.Dept.DeptName} | {x.Email} | {x.Status}");

        var bytes = SimplePdfExporter.CreatePdf("Employee Report", lines);
        return File(bytes, "application/pdf", $"employees_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    private IQueryable<Employee> BuildFilteredQuery(string? search)
    {
        var query = _context.Employees.Include(x => x.Dept).Include(x => x.Pos).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Email.Contains(keyword) ||
                x.PhoneNumber.Contains(keyword) ||
                x.Status.Contains(keyword));
        }

        return query;
    }

    private async Task<EmployeeFormViewModel> BuildEmployeeFormAsync(EmployeeFormViewModel model, int? employeeId = null)
    {
        var departments = await _context.Departments
            .Include(x => x.DeptManager)
            .OrderBy(x => x.DeptName)
            .Select(x => new SelectListItem
            {
                Value = x.DeptId.ToString(),
                Text = x.DeptName,
                Selected = model.DeptId == x.DeptId
            })
            .ToListAsync();

        // Tự động load tên Manager từ phòng ban đã chọn (không cho chọn manager thủ công)
        if (model.DeptId > 0)
        {
            var deptWithManager = await _context.Departments
                .Include(x => x.DeptManager)
                .FirstOrDefaultAsync(x => x.DeptId == model.DeptId);
            if (deptWithManager != null)
            {
                model.ManagerId = deptWithManager.DeptManagerId;
                model.ManagerName = deptWithManager.DeptManager?.Name ?? "(Chưa có manager)";
            }
        }

        var positions = await _context.Positions
            .OrderBy(x => x.PosName)
            .Select(x => new SelectListItem
            {
                Value = x.PosId.ToString(),
                Text = x.PosName,
                Selected = model.PositionId == x.PosId
            })
            .ToListAsync();

        var statusValues = new[] { "Working", "On leave", "Resigned" };
        model.StatusOptions = statusValues.Select(s => new SelectListItem
        {
            Value = s,
            Text = s,
            Selected = string.Equals(model.Status, s, StringComparison.OrdinalIgnoreCase)
        }).ToList();

        if (string.IsNullOrWhiteSpace(model.Status) || !statusValues.Contains(model.Status, StringComparer.OrdinalIgnoreCase))
        {
            model.Status = "Working";
        }

        model.Departments = departments;
        model.Positions = positions;
        model.CanEditPosition = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);

        if (model.PositionId.HasValue && model.PositionId > 0)
        {
            model.PositionName = await _context.Positions
                .Where(x => x.PosId == model.PositionId.Value)
                .Select(x => x.PosName)
                .FirstOrDefaultAsync();
        }

        return model;
    }

    /// <summary>
    /// API endpoint trả về thông tin Manager của phòng ban (dùng cho AJAX khi chọn phòng ban trong form)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDeptManager(int deptId)
    {
        var dept = await _context.Departments
            .Include(x => x.DeptManager)
            .FirstOrDefaultAsync(x => x.DeptId == deptId);

        if (dept == null)
            return NotFound();

        return Json(new
        {
            managerId = dept.DeptManagerId,
            managerName = dept.DeptManager?.Name ?? "(Chưa có manager)"
        });
    }
}
