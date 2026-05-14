using HRM.Web.Models.Entities;

namespace HRM.Web.Models.ViewModels.Departments;

public class DepartmentMembersViewModel
{
    public int DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public IReadOnlyList<Employee> CurrentEmployees { get; set; } = Array.Empty<Employee>();
    public IReadOnlyList<Employee> AvailableEmployees { get; set; } = Array.Empty<Employee>();
}
