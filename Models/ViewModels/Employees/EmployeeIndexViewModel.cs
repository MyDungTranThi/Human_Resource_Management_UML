using HRM.Web.Helpers;
using HRM.Web.Models.Entities;

namespace HRM.Web.Models.ViewModels.Employees;

public class EmployeeIndexViewModel
{
    public string? Search { get; set; }
    public int PageSize { get; set; } = 10;
    public PagedResult<Employee> PagedEmployees { get; set; } = new();
    public IReadOnlyList<EmployeeDepartmentStatViewModel> DepartmentStats { get; set; } = Array.Empty<EmployeeDepartmentStatViewModel>();
}

public class EmployeeDepartmentStatViewModel
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
}
