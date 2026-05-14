using HRM.Web.Helpers;
using HRM.Web.Models.Entities;

namespace HRM.Web.Models.ViewModels.Projects;

public class ProjectIndexViewModel
{
    public string? Search { get; set; }
    public int PageSize { get; set; } = 10;
    public PagedResult<Project> PagedProjects { get; set; } = new();
}
