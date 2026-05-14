using HRM.Web.Helpers;
using HRM.Web.Models.Entities;

namespace HRM.Web.Models.ViewModels.Departments;

public class DepartmentIndexViewModel
{
    public string? Search { get; set; }
    public int PageSize { get; set; } = 10;
    public PagedResult<Department> PagedDepartments { get; set; } = new();
}
