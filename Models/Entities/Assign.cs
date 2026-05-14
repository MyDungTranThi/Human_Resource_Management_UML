using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Assign
{
    public int EmpId { get; set; }

    public int ProjId { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }


    public virtual Employee Emp { get; set; } = null!;

    public virtual Project Proj { get; set; } = null!;
}
