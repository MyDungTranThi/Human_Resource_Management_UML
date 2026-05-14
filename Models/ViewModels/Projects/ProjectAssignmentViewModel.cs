using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models.ViewModels.Projects;

public class ProjectAssignmentViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Please choose an employee.")]
    public int EmpId { get; set; }

    public TimeOnly StartTime { get; set; } = new(8, 0);

    public TimeOnly EndTime { get; set; } = new(17, 0);

    public IEnumerable<SelectListItem> Employees { get; set; } = Array.Empty<SelectListItem>();
}
