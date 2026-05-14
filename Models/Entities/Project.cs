using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Project
{
    public int ProjId { get; set; }

    public string ProjName { get; set; } = null!;

    public DateOnly ProjStartDate { get; set; }

    public DateOnly? ProjEndDate { get; set; }

    public string ProjStatus { get; set; } = null!;

    public double Budget { get; set; }

    public int AssignedByManagerId { get; set; }

    public virtual ICollection<Assign> Assigns { get; set; } = new List<Assign>();

    public virtual ICollection<Reward> Rewards { get; set; } = new List<Reward>();


    public virtual ICollection<Department> Depts { get; set; } = new List<Department>();
}
