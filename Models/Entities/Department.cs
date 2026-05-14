using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Department
{
    public int DeptId { get; set; }

    public string DeptName { get; set; } = null!;

    public int DeptManagerId { get; set; }

    public virtual Employee DeptManager { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Project> Projs { get; set; } = new List<Project>();
}
