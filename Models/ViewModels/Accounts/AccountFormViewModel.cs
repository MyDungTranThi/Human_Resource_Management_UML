using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models.ViewModels.Accounts;

public class AccountFormViewModel
{
    public int? AccountId { get; set; }

    [Display(Name = "Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự.")]
    public string? Password { get; set; }

    [Display(Name = "Nhân viên")]
    public int? EmpId { get; set; }

    [Display(Name = "Phân quyền")]
    [Required(ErrorMessage = "Vui lòng chọn phân quyền.")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; } = true;

    // Các SelectListItems dùng để hiển thị dropdown
    public IEnumerable<SelectListItem>? Employees { get; set; }
    public IEnumerable<SelectListItem>? Roles { get; set; }
}
