using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Absence
{
    public int AbsenceId { get; set; }

    public int EmpId { get; set; }

    public DateOnly AbsenceDate { get; set; }

    public string? Reason { get; set; }

    public bool IsUnpaid { get; set; } = true;

    public virtual Employee Emp { get; set; } = null!;
}
