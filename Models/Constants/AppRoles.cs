namespace HRM.Web.Models.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string DeptHead = "DeptHead";
    public const string HR = "HR";
    public const string Accountant = "Accountant";
    public const string Staff = "Staff";

    public const string HROrAdmin = $"{HR},{Admin}";
    public const string AccountingOrAdmin = $"{Accountant},{Admin}";
    public const string ManagerOrAdmin = $"{Manager},{Admin}";
    public const string DeptHeadOrAdmin = $"{DeptHead},{Manager},{Admin}";
}
