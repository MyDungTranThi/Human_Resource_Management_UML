namespace HRM.Web.Models.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int EmployeeCount { get; set; }
    public int DepartmentCount { get; set; }
    public int ProjectCount { get; set; }
    public int ActiveProjectCount { get; set; }
    public decimal ThisMonthPayrollTotal { get; set; }
    public string CurrentRole { get; set; } = string.Empty;
}
