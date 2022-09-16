using CoralTime.ViewModels.Member;
using CoralTime.ViewModels.Projects;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;

namespace CoralTime.BL.Interfaces
{
    public interface IProjectService
    {
        IEnumerable<ProjectView> TimeTrackerAllProjects();

        IEnumerable<ManagerProjectsView> ManageProjectsOfManager();

        ProjectView GetById(int id);

        ProjectView Update(int id, JsonElement projectView);

        ProjectView Create(JsonElement newProject);

        ProjectView Patch(int id, JsonElement projectView);

        IEnumerable<MemberView> GetMembers(int projectId);

        IEnumerable<ProjectNameView> GetProjectsNames();

        bool Delete(int id);
    }
}