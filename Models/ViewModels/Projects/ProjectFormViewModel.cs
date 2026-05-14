using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models.ViewModels.Projects;

public class ProjectFormViewModel
{
    public int? ProjId { get; set; }

    [Display(Name = "Tên dự án")]
    [Required(ErrorMessage = "Vui lòng nhập tên dự án.")]
    [StringLength(100)]
    public string ProjName { get; set; } = string.Empty;

    [Display(Name = "Ngày bắt đầu")]
    [DataType(DataType.Date)]
    public DateOnly ProjStartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Ngày kết thúc")]
    [DataType(DataType.Date)]
    public DateOnly? ProjEndDate { get; set; }

    [Display(Name = "Trạng thái")]
    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [StringLength(50)]
    public string ProjStatus { get; set; } = "Not started";

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Array.Empty<SelectListItem>();

    [Display(Name = "Ngân sách")]
    [Range(0, double.MaxValue, ErrorMessage = "Ngân sách phải >= 0.")]
    public double Budget { get; set; }

    [Display(Name = "Phòng ban phụ trách")]
    [Required(ErrorMessage = "Vui lòng chọn phòng ban.")]
    public int DeptId { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = Array.Empty<SelectListItem>();

}
