namespace HRM.Web.Models.ViewModels.Reports;

public class ReportsDashboardViewModel
{
    // Role flags
    public bool ShowHrMetrics { get; set; }
    public bool ShowAccountantMetrics { get; set; }
    public bool ShowManagerMetrics { get; set; }

    // HR Metrics
    public int TotalHeadcount { get; set; }
    public string? MostAbsentEmployeeName { get; set; }
    public int MostAbsentCount { get; set; }
    public IReadOnlyList<HrEmployeeReportItem> HrEmployeeList { get; set; } = Array.Empty<HrEmployeeReportItem>();
    public IReadOnlyList<HrAbsenceReportItem> HrAbsenceList { get; set; } = Array.Empty<HrAbsenceReportItem>();

    // Accountant Metrics
    public decimal PayrollCost { get; set; }
    public IReadOnlyList<SalaryRankingItem> SalaryRanking { get; set; } = Array.Empty<SalaryRankingItem>();

    // Manager Metrics
    public int TotalDepartments { get; set; }
    public int TotalProjects { get; set; }
    public IReadOnlyList<ManagerDepartmentItem> ManagerDepartmentList { get; set; } = Array.Empty<ManagerDepartmentItem>();
    public IReadOnlyList<ManagerProjectItem> ManagerProjectList { get; set; } = Array.Empty<ManagerProjectItem>();

    // Existing / Shared / Admin Metrics
    public int ActiveProjects { get; set; }
    public decimal AverageTenureYears { get; set; }
    public IReadOnlyList<MonthlyPayrollPoint> MonthlyPayrollTrend { get; set; } = Array.Empty<MonthlyPayrollPoint>();
    public IReadOnlyList<DepartmentRetentionItem> DepartmentRetention { get; set; } = Array.Empty<DepartmentRetentionItem>();
}

public class MonthlyPayrollPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Payroll { get; set; }
    public decimal Budget { get; set; }
}

public class DepartmentRetentionItem
{
    public string DepartmentName { get; set; } = string.Empty;
    public decimal RetentionPercent { get; set; }
}

public class SalaryRankingItem
{
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Salary { get; set; }
}

public class HrEmployeeReportItem
{
    public int EmpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class HrAbsenceReportItem
{
    public string EmployeeName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class ManagerDepartmentItem
{
    public string DeptName { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
}

public class ManagerProjectItem
{
    public string ProjName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Budget { get; set; }
}
