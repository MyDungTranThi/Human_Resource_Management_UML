using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int EmpId { get; set; }

    public DateOnly WorkDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Shift { get; set; } = null!;

    public virtual Employee Emp { get; set; } = null!;
}
