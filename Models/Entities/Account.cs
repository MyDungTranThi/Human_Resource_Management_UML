using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int? EmpId { get; set; }

    public string Role { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual Employee? Emp { get; set; }
}
