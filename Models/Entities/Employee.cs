using System;
using System.Collections.Generic;

namespace HRM.Web.Models.Entities;

public partial class Employee
{
    public int EmpId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public bool Gender { get; set; }

    public string Address { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int DeptId { get; set; }

    public decimal AnnualLeaveDays { get; set; }

    public string Status { get; set; } = null!;

    public string? ImagePath { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Assign> Assigns { get; set; } = new List<Assign>();

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual Department Dept { get; set; } = null!;

    public virtual ICollection<EmploymentContract> EmploymentContracts { get; set; } = new List<EmploymentContract>();


    public virtual ICollection<Reward> Rewards { get; set; } = new List<Reward>();

    public virtual ICollection<Salary> Salaries { get; set; } = new List<Salary>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Timekeeping> Timekeepings { get; set; } = new List<Timekeeping>();

    public virtual ICollection<Position> Pos { get; set; } = new List<Position>();

    public virtual ICollection<Absence> Absences { get; set; } = new List<Absence>();
}
