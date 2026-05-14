using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models.ViewModels.Departments;

public class DepartmentFormViewModel
{
    public int? DeptId { get; set; }
    
    [Display(Name = "Tên phòng ban")]
    [Required(ErrorMessage = "Vui lòng nhập tên phòng ban.")]
    [StringLength(50)]
    public string DeptName { get; set; } = string.Empty;

    [Display(Name = "Trưởng phòng")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn trưởng phòng.")]
    public int DeptManagerId { get; set; }

    public IEnumerable<SelectListItem> Managers { get; set; } = Array.Empty<SelectListItem>();
}
