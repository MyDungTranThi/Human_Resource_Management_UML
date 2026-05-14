namespace HRM.Web.Models.ViewModels.Payroll;

public class PayrollIndexViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string? Search { get; set; }
    public IReadOnlyList<PayrollLineViewModel> Lines { get; set; } = Array.Empty<PayrollLineViewModel>();
    public decimal TotalNetSalary => Lines.Sum(x => x.NetSalary);
}
