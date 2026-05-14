using HRM.Web.Helpers;
using HRM.Web.Models.Constants;
using HRM.Web.Models.Entities;
using HRM.Web.Models.ViewModels.Accounts;
using HRM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HRM.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AccountsController : Controller
{
    private readonly HRMDbContext _context;

    public AccountsController(HRMDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 5 or > 100 ? 10 : pageSize;

        var query = _context.Accounts
            .Include(x => x.Emp)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => 
                x.Username.Contains(keyword) || 
                (x.Emp != null && x.Emp.Name.Contains(keyword)));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.AccountId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new AccountIndexViewModel
        {
            Search = search,
            PageSize = pageSize,
            PagedAccounts = new PagedResult<Account>
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
        var model = await BuildFormAsync(new AccountFormViewModel());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu khi tạo tài khoản mới.");
        }

        if (await _context.Accounts.AnyAsync(x => x.Username == model.Username.Trim()))
        {
            ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
        }

        if (model.EmpId.HasValue && await _context.Accounts.AnyAsync(x => x.EmpId == model.EmpId))
        {
            ModelState.AddModelError("EmpId", "Nhân viên này đã được liên kết với một tài khoản khác.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var account = new Account
        {
            Username = model.Username.Trim(),
            PasswordHash = PasswordHasher.Hash(model.Password!),
            EmpId = model.EmpId,
            Role = model.Role,
            IsActive = model.IsActive
        };

        _context.Accounts.Add(account);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã tạo tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi khi tạo tài khoản: {ex.Message}");
            return View(await BuildFormAsync(model));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(x => x.AccountId == id);
        if (account == null)
        {
            return NotFound();
        }

        var model = new AccountFormViewModel
        {
            AccountId = account.AccountId,
            Username = account.Username,
            EmpId = account.EmpId,
            Role = account.Role,
            IsActive = account.IsActive ?? true
        };

        model = await BuildFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountFormViewModel model)
    {
        if (model.AccountId != id)
        {
            return BadRequest();
        }

        if (await _context.Accounts.AnyAsync(x => x.Username == model.Username.Trim() && x.AccountId != id))
        {
            ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
        }

        if (model.EmpId.HasValue && await _context.Accounts.AnyAsync(x => x.EmpId == model.EmpId && x.AccountId != id))
        {
            ModelState.AddModelError("EmpId", "Nhân viên này đã được liên kết với một tài khoản khác.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var account = await _context.Accounts.FirstOrDefaultAsync(x => x.AccountId == id);
        if (account == null)
        {
            return NotFound();
        }

        account.Username = model.Username.Trim();
        account.EmpId = model.EmpId;
        account.Role = model.Role;
        account.IsActive = model.IsActive;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            account.PasswordHash = PasswordHasher.Hash(model.Password);
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
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
        var account = await _context.Accounts.FirstOrDefaultAsync(x => x.AccountId == id);
        if (account == null)
        {
            TempData["Error"] = "Không tìm thấy tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        _context.Accounts.Remove(account);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa tài khoản thành công.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể xóa tài khoản này.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<AccountFormViewModel> BuildFormAsync(AccountFormViewModel model)
    {
        // Get employees who don't have an account (except the one currently being edited)
        var accounts = await _context.Accounts.ToListAsync();
        var takenEmpIds = accounts
            .Where(a => a.EmpId.HasValue && a.AccountId != model.AccountId)
            .Select(a => a.EmpId.Value)
            .ToList();

        var employees = await _context.Employees
            .Where(e => !takenEmpIds.Contains(e.EmpId))
            .OrderBy(e => e.Name)
            .ToListAsync();

        model.Employees = employees.Select(x => new SelectListItem
        {
            Value = x.EmpId.ToString(),
            Text = $"{x.Name} (#{x.EmpId})",
            Selected = model.EmpId == x.EmpId
        }).ToList();

        model.Roles = new List<SelectListItem>
        {
            new SelectListItem { Value = AppRoles.Admin, Text = "Quản trị viên (Admin)", Selected = model.Role == AppRoles.Admin },
            new SelectListItem { Value = AppRoles.Manager, Text = "Quản lý (Manager)", Selected = model.Role == AppRoles.Manager },
            new SelectListItem { Value = AppRoles.DeptHead, Text = "Trưởng phòng (DeptHead)", Selected = model.Role == AppRoles.DeptHead },
            new SelectListItem { Value = AppRoles.HR, Text = "Nhân sự (HR)", Selected = model.Role == AppRoles.HR },
            new SelectListItem { Value = AppRoles.Accountant, Text = "Kế toán (Accountant)", Selected = model.Role == AppRoles.Accountant },
            new SelectListItem { Value = AppRoles.Staff, Text = "Nhân viên (Staff)", Selected = model.Role == AppRoles.Staff }
        };

        return model;
    }
}
