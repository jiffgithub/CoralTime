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

        ProjectView Update(int id, dynamic projectView);

        ProjectView Create(dynamic newProject);

        ProjectView Patch(int id, dynamic projectView);

        IEnumerable<MemberView> GetMembers(int projectId);

        IEnumerable<ProjectNameView> GetProjectsNames();

        bool Delete(int id);
    }
}