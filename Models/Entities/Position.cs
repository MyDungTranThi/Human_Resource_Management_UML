using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Position
{
    public int PosId { get; set; }

    public string PosName { get; set; } = null!;

    public decimal BaseSalary { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Employee> Emps { get; set; } = new List<Employee>();
}
