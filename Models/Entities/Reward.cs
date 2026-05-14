using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Reward
{
    public int RewardId { get; set; }

    public int EmpId { get; set; }

    public int ProjId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public string RewardType { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public decimal Value { get; set; }

    public virtual Employee Emp { get; set; } = null!;

    public virtual Project Proj { get; set; } = null!;
}
