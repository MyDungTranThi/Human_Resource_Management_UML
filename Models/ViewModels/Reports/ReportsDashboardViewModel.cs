namespace HRM.Web.Models.ViewModels.Reports;

public class ReportsDashboardViewModel
{
    public int TotalHeadcount { get; set; }
    public decimal PayrollCost { get; set; }
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
