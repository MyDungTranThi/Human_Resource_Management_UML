using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Salary
{
    public int EmpId { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public decimal BaseSalary { get; set; }

    public decimal Bonus { get; set; }

    public decimal Deduction { get; set; }

    public decimal NetSalary { get; set; }

    public decimal LeaveDaysInMonth { get; set; }

    public virtual Employee Emp { get; set; } = null!;
}
