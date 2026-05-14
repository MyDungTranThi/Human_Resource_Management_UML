using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models.ViewModels.Employees;

public class EmployeeFormViewModel
{
    public int? EmpId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-20));

    public bool Gender { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(100)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a department.")]
    public int DeptId { get; set; }

    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }

    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public bool CanEditPosition { get; set; }

    [Range(0, 100, ErrorMessage = "Annual leave days is invalid.")]
    public decimal AnnualLeaveDays { get; set; } = 12;

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50)]
    public string Status { get; set; } = "Working";

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Array.Empty<SelectListItem>();

    public string? ImagePath { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Managers { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Positions { get; set; } = Array.Empty<SelectListItem>();
}
