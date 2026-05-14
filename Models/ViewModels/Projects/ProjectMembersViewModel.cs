using HRM.Web.Models.Entities;

namespace HRM.Web.Models.ViewModels.Projects;

public class ProjectMembersViewModel
{
    public int ProjId { get; set; }
    public string ProjName { get; set; } = string.Empty;
    public IReadOnlyList<Assign> Assignments { get; set; } = Array.Empty<Assign>();
    public ProjectAssignmentViewModel AssignmentForm { get; set; } = new();
}
