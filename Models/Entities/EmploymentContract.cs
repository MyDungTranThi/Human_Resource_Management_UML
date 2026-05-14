using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class EmploymentContract
{
    public int ContId { get; set; }

    public int EmpId { get; set; }

    public DateOnly SigningDate { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string ContractType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual Employee Emp { get; set; } = null!;
}
