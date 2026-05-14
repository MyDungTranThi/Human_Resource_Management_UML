using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models.ViewModels.Absences;

public class AbsenceFormViewModel
{
    public int? AbsenceId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhân viên.")]
    public int EmpId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày nghỉ.")]
    public DateOnly AbsenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(200)]
    public string? Reason { get; set; }

    public bool IsUnpaid { get; set; } = true;

    public IEnumerable<SelectListItem> Employees { get; set; } = Array.Empty<SelectListItem>();
}

public class AbsenceIndexViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string? Search { get; set; }
    public List<HRM.Web.Models.Entities.Absence> Items { get; set; } = new();
}
