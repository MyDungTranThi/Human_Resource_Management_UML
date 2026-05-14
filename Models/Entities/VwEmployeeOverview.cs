using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class VwEmployeeOverview
{
    public int EmpId { get; set; }

    public string EmployeeName { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Department { get; set; }

    public string? Position { get; set; }

    public string Status { get; set; } = null!;

    public int? NumberOfProjects { get; set; }
}
