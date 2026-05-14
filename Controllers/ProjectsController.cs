using ClosedXML.Excel;
using HRM.Web.Helpers;
using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Projects;
using HRM.Web.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRM.Web.Controllers;

[Authorize(Roles = AppRoles.ManagerOrAdmin)]
public class ProjectsController : Controller
{
    private readonly HRMDbContext _context;

    public ProjectsController(HRMDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 5 or > 100 ? 10 : pageSize;

        var query = _context.Projects
            .Include(x => x.Assigns)
                .ThenInclude(x => x.Emp)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.ProjName.Contains(keyword) ||
                x.ProjStatus.Contains(keyword));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.ProjId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new ProjectIndexViewModel
        {
            Search = search,
            PageSize = pageSize,
            PagedProjects = new PagedResult<Project>
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
        var model = await BuildFormAsync(new ProjectFormViewModel());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var allowedStatuses = new[] { "On Going", "Not started", "Done" };
        if (string.IsNullOrWhiteSpace(model.ProjStatus) || !allowedStatuses.Contains(model.ProjStatus.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.ProjStatus), "Trạng thái dự án không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var dept = await _context.Departments.FindAsync(model.DeptId);
        var project = new Project
        {
            ProjName = model.ProjName.Trim(),
            ProjStartDate = model.ProjStartDate,
            ProjEndDate = model.ProjEndDate,
            ProjStatus = model.ProjStatus.Trim(),
            Budget = model.Budget,
            AssignedByManagerId = dept?.DeptManagerId ?? 0 // Lấy ID Trưởng phòng gán vào
        };

        if (dept != null)
        {
            project.Depts.Add(dept);
        }

        _context.Projects.Add(project);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm dự án mới thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi DB: {ex.Message} {(ex.InnerException != null ? " -> " + ex.InnerException.Message : "")}");
            return View(await BuildFormAsync(model));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _context.Projects
            .Include(x => x.Depts)
            .FirstOrDefaultAsync(x => x.ProjId == id);
        if (project == null)
        {
            return NotFound();
        }

        var model = new ProjectFormViewModel
        {
            ProjId = project.ProjId,
            ProjName = project.ProjName,
            ProjStartDate = project.ProjStartDate,
            ProjEndDate = project.ProjEndDate,
            ProjStatus = project.ProjStatus,
            Budget = project.Budget,
            DeptId = project.Depts.FirstOrDefault()?.DeptId ?? 0
        };

        model = await BuildFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProjectFormViewModel model)
    {
        if (model.ProjId != id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var allowedStatuses = new[] { "On Going", "Not started", "Done" };
        if (string.IsNullOrWhiteSpace(model.ProjStatus) || !allowedStatuses.Contains(model.ProjStatus.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.ProjStatus), "Trạng thái dự án không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var project = await _context.Projects
            .Include(x => x.Depts)
            .FirstOrDefaultAsync(x => x.ProjId == id);

        if (project == null)
        {
            return NotFound();
        }

        var dept = await _context.Departments.FindAsync(model.DeptId);
        project.ProjName = model.ProjName.Trim();
        project.ProjStartDate = model.ProjStartDate;
        project.ProjEndDate = model.ProjEndDate;
        project.ProjStatus = model.ProjStatus.Trim();
        project.Budget = model.Budget;
        project.AssignedByManagerId = dept?.DeptManagerId ?? project.AssignedByManagerId;

        project.Depts.Clear();
        if (dept != null)
        {
            project.Depts.Add(dept);
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật thông tin dự án thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi cập nhật: {ex.Message}");
            return View(await BuildFormAsync(model));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(x => x.ProjId == id);
        if (project == null)
        {
            TempData["Error"] = "Project not found.";
            return RedirectToAction(nameof(Index));
        }

        _context.Projects.Remove(project);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Deleted project successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Could not delete project due to related data.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Members(int id)
    {
        var project = await _context.Projects
            .Include(x => x.Assigns)
                .ThenInclude(x => x.Emp)
            .FirstOrDefaultAsync(x => x.ProjId == id);

        if (project == null)
        {
            return NotFound();
        }

        var assignedEmployeeIds = project.Assigns.Select(x => x.EmpId).ToHashSet();

        var candidates = await _context.Employees
            .Where(x => !assignedEmployeeIds.Contains(x.EmpId))
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.EmpId.ToString(),
                Text = $"{x.Name} (#{x.EmpId})"
            })
            .ToListAsync();

        var model = new ProjectMembersViewModel
        {
            ProjId = project.ProjId,
            ProjName = project.ProjName,
            Assignments = project.Assigns.OrderBy(x => x.Emp.Name).ToList(),
            AssignmentForm = new ProjectAssignmentViewModel
            {
                Employees = candidates
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int id, ProjectAssignmentViewModel form)
    {
        if (form.EmpId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn nhân viên.";
            return RedirectToAction(nameof(Members), new { id });
        }

        if (form.StartTime >= form.EndTime)
        {
            TempData["Error"] = "Thời gian bắt đầu phải trước thời gian kết thúc.";
            return RedirectToAction(nameof(Members), new { id });
        }

        var exists = await _context.Assigns.AnyAsync(x => x.ProjId == id && x.EmpId == form.EmpId);
        if (exists)
        {
            TempData["Error"] = "Nhân viên này đã có trong dự án.";
            return RedirectToAction(nameof(Members), new { id });
        }

        _context.Assigns.Add(new Assign
        {
            ProjId = id,
            EmpId = form.EmpId,
            StartTime = form.StartTime,
            EndTime = form.EndTime
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "Added member to project.";
        return RedirectToAction(nameof(Members), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, int empId)
    {
        var assignment = await _context.Assigns.FirstOrDefaultAsync(x => x.ProjId == id && x.EmpId == empId);
        if (assignment == null)
        {
            TempData["Error"] = "Assignment not found.";
            return RedirectToAction(nameof(Members), new { id });
        }

        _context.Assigns.Remove(assignment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Removed member from project.";
        return RedirectToAction(nameof(Members), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var data = await _context.Projects
            .Include(x => x.Assigns)
            .OrderBy(x => x.ProjId)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Projects");

        sheet.Cell(1, 1).Value = "ProjId";
        sheet.Cell(1, 2).Value = "ProjectName";
        sheet.Cell(1, 3).Value = "Status";
        sheet.Cell(1, 4).Value = "Manager";
        sheet.Cell(1, 5).Value = "StartDate";
        sheet.Cell(1, 6).Value = "EndDate";
        sheet.Cell(1, 7).Value = "Budget";
        sheet.Cell(1, 8).Value = "Members";

        for (var i = 0; i < data.Count; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = data[i].ProjId;
            sheet.Cell(row, 2).Value = data[i].ProjName;
            sheet.Cell(row, 3).Value = data[i].ProjStatus;
            sheet.Cell(row, 4).Value = "N/A";
            sheet.Cell(row, 5).Value = data[i].ProjStartDate.ToString("yyyy-MM-dd");
            sheet.Cell(row, 6).Value = data[i].ProjEndDate?.ToString("yyyy-MM-dd") ?? "";
            sheet.Cell(row, 7).Value = data[i].Budget;
            sheet.Cell(row, 8).Value = data[i].Assigns.Count;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"projects_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var data = await _context.Projects
            .Include(x => x.Assigns)
            .OrderBy(x => x.ProjId)
            .Take(100)
            .ToListAsync();

        var lines = data.Select(x =>
            $"#{x.ProjId} | {x.ProjName} | {x.ProjStatus} | Members: {x.Assigns.Count}");

        var bytes = SimplePdfExporter.CreatePdf("Project Report", lines);
        return File(bytes, "application/pdf", $"projects_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    private async Task<ProjectFormViewModel> BuildFormAsync(ProjectFormViewModel model)
    {
        model.Departments = await _context.Departments
            .OrderBy(x => x.DeptName)
            .Select(x => new SelectListItem
            {
                Value = x.DeptId.ToString(),
                Text = x.DeptName,
                Selected = x.DeptId == model.DeptId
            })
            .ToListAsync();

        var statusValues = new[] { "On Going", "Not started", "Done" };
        model.StatusOptions = statusValues.Select(s => new SelectListItem
        {
            Value = s,
            Text = s,
            Selected = string.Equals(model.ProjStatus, s, StringComparison.OrdinalIgnoreCase)
        }).ToList();

        if (string.IsNullOrWhiteSpace(model.ProjStatus) || !statusValues.Contains(model.ProjStatus, StringComparer.OrdinalIgnoreCase))
        {
            model.ProjStatus = "Not started";
        }

        return model;
    }
}
