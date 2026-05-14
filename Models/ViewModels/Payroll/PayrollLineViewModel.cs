namespace HRM.Web.Models.ViewModels.Payroll;

public class PayrollLineViewModel
{
    public int EmpId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal Bonus { get; set; }
    public decimal Deduction { get; set; }
    public decimal NetSalary { get; set; }
    public decimal LeaveDaysInMonth { get; set; }
    public bool IsPaid { get; set; }
}
