namespace HRM.Web.Models.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int EmployeeCount { get; set; }
    public int DepartmentCount { get; set; }
    public int ProjectCount { get; set; }
    public int ActiveProjectCount { get; set; }
    public decimal ThisMonthPayrollTotal { get; set; }
    public string CurrentRole { get; set; } = string.Empty;

    public List<MonthlySalaryStat> SalaryTrend { get; set; } = new();
    public List<DepartmentStat> DeptDistribution { get; set; } = new();
    public List<RecentActivityViewModel> Activities { get; set; } = new();
}

public class MonthlySalaryStat
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthLabel => $"Thg {Month}";
    public decimal Total { get; set; }
}

public class DepartmentStat
{
    public string DeptName { get; set; } = null!;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class RecentActivityViewModel
{
    public string EmployeeName { get; set; } = null!;
    public string DeptName { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string DateLabel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string StatusClass { get; set; } = null!;
}
