using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Timekeeping
{
    public int EmpId { get; set; }

    public DateOnly WorkDate { get; set; }

    public TimeOnly CheckInTime { get; set; }

    public TimeOnly CheckOutTime { get; set; }

    public double WorkingHours { get; set; }

    public virtual Employee Emp { get; set; } = null!;
}
