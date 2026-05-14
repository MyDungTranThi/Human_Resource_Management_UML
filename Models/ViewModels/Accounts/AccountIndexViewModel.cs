using HRM.Web.Helpers;
using HRM.Web.Models.Entities;

namespace HRM.Web.Models.ViewModels.Accounts;

public class AccountIndexViewModel
{
    public string? Search { get; set; }
    public int PageSize { get; set; }
    public PagedResult<Account> PagedAccounts { get; set; } = new();
}
