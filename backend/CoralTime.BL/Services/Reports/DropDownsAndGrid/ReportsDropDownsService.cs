﻿using System;
using CoralTime.Common.Constants;
using CoralTime.DAL.ConvertModelToView;
using CoralTime.DAL.Models;
using CoralTime.ViewModels.Reports.Request.Grid;
using CoralTime.ViewModels.Reports.Responce.DropDowns.Filters;
using CoralTime.ViewModels.Reports.Responce.DropDowns.GroupBy;
using System.Collections.Generic;
using System.Linq;
using CoralTime.Common.Helpers;
using CoralTime.Common.Exceptions;
using CoralTime.DAL.ConvertViewToModel;

namespace CoralTime.BL.Services.Reports.DropDownsAndGrid
{
    public partial class ReportsService
    {
        #region Values of groupByInfo, showColumnsInfo, datesStaticInfo.

        private readonly ReportCommonDropDownsView[] groupByInfo =
        {
            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ReportsGroupByIds.Project,
                Description = Constants.ReportsGroupByIds.Project.ToString()
            },

            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ReportsGroupByIds.User,
                Description = Constants.ReportsGroupByIds.User.ToString()
            },

            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ReportsGroupByIds.Date,
                Description = Constants.ReportsGroupByIds.Date.ToString()
            },

            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ReportsGroupByIds.Client,
                Description = Constants.ReportsGroupByIds.Client.ToString()
            }
        };

        private readonly ReportCommonDropDownsView[] showColumnsInfo =
        {
            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ShowColumnModelIds.ShowEstimatedTime,
                Description = "Show Estimated Hours"
            },
            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ShowColumnModelIds.ShowDate,
                Description = "Show Date"
            },
            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ShowColumnModelIds.ShowNotes,
                Description = "Show Notes"
            },
            new ReportCommonDropDownsView
            {
                Id = (int) Constants.ShowColumnModelIds.ShowStartFinish,
                Description = "Show Start/Finish Time"
            }
        };

        #endregion

        public ReportDropDownView GetReportsDropDowns()
        {
            return new ReportDropDownView
            {
                CurrentQuery = GetCurrentQuery() ?? CreateQueryWithDefaultValues(),
                Values = CreateDropDownValues()
            };
        }

        private ReportDropDownValues CreateDropDownValues()
        {
            return new ReportDropDownValues
            {
                Filters = CreateFilters(),
                UserDetails = CreateUserDetails(),
                CustomQueries = GetCustomQueries().OrderBy(x => x.QueryName).ToList(),
                GroupBy = groupByInfo,
                ShowColumns = showColumnsInfo,
                DateStatic = GetDatesStaticInfo()
            };
        }

        private List<Client> CreateClients()
        {
            var projects = new List<Project>();
            var clients = new List<Client>();

            #region GetProjects allProjectsForAdmin or projectsWithAssignUsersAndPublicProjects.

            if (BaseMemberImpersonated.User.IsAdmin)
            {
                var allProjectsForAdmin = Uow.ProjectRepository.LinkedCacheGetList().ToList();
                projects = allProjectsForAdmin;
            }
            else
            {
                var projectsWithAssignUsersAndPublicProjects = Uow.ProjectRepository.LinkedCacheGetList()
                    .Where(x => x.MemberProjectRoles.Select(z => z.MemberId).Contains(BaseMemberImpersonated.Id) || !x.IsPrivate)
                    .ToList();

                projects = projectsWithAssignUsersAndPublicProjects;
            }

            #endregion

            #region Get Clients from Projects of clients.

            // 1. Get all clients from targeted projects where project is assign to client.
            var clientsWithProjects = projects.Where(project => project.Client != null)
                .Select(project => project.Client)
                .Distinct()
                .Select(client => new Client
                {
                    Id = client.Id,
                    Name = client.Name,
                    Email = client.Email,
                    IsActive = client.IsActive,
                    Description = client.Description,
                    Projects = new List<Project>(client.Projects.Where(projectOfClient => projects.Select(project => project.Id).Contains(projectOfClient.Id)).ToList())
                }).ToList();

            clients.AddRange(clientsWithProjects);

            // 2. Get all projects where project is not assign to client and create client "WithoutClients" that we add projects to it.
            var hasClientsWithoutProjects = projects.Where(x => x.Client == null).Any();
            if (hasClientsWithoutProjects)
            {
                var clientWithoutProjects = new Client
                {
                    Id = Constants.WithoutClient.Id,
                    Name = Constants.WithoutClient.Name,
                    IsActive = true,
                    Projects = new List<Project>(projects.Where(x => x.Client == null).ToList())
                };

                clients.Add(clientWithoutProjects);
            }

            #endregion

            return clients;
        }

        private List<ReportClientView> CreateFilters()
        {
            var reportClientView = new List<ReportClientView>();
            var members = Uow.MemberRepository.LinkedCacheGetList();

            var managerRoleId = Uow.ProjectRoleRepository.GetManagerRoleId();
            var memberRoleId = Uow.ProjectRoleRepository.GetMemberRoleId();

            var clients = CreateClients();
            foreach (var client in clients)
            {
                var reportProjectViewByUserId = new List<ReportProjectView>();

                foreach (var project in client.Projects)
                {
                    var reportProjectView = new ReportProjectView
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        RoleId = project.MemberProjectRoles.FirstOrDefault(r => r.MemberId == ReportMemberImpersonated.Id)?.RoleId ?? 0,
                        IsProjectActive = project.IsActive,
                    };

                    #region Set all users at Project constrain only for: Admin, Manager at this project.

                    var isManagerOnProject = project.MemberProjectRoles.Exists(r => r.MemberId == ReportMemberImpersonated.Id && r.RoleId == managerRoleId);

                    if (ReportMemberImpersonated.User.IsAdmin || isManagerOnProject)
                    {
                        var usersDetailsView = project.MemberProjectRoles.Select(x => x.Member.GetViewReportUsers(x.RoleId, Mapper)).ToList();

                        // Add members, that is not assigned to this project directly.
                        if (!project.IsPrivate)
                        {
                            var notAssignedMembersAtProjView = members.Where(x => project.MemberProjectRoles.Select(y => y.MemberId).All(mi => x.Id != mi)).Select(u => u.GetViewReportUsers(memberRoleId, Mapper));
                            usersDetailsView.AddRange(notAssignedMembersAtProjView);
                        }

                        // Set all users of the project.
                        reportProjectView.UsersDetails = usersDetailsView;
                    }

                    #endregion

                    reportProjectView.IsUserManagerOnProject = isManagerOnProject;
                    reportProjectViewByUserId.Add(reportProjectView);
                }

                var reportClientViewlocal = new ReportClientView
                {
                    ClientId = client.Id,
                    ClientName = client.Name,
                    IsClientActive = client.IsActive,
                    ProjectsDetails = reportProjectViewByUserId
                };

                reportClientView.Add(reportClientViewlocal);
            }

            return reportClientView;
        }

        private ReportUserDetails CreateUserDetails()
        {
            return new ReportUserDetails
            {
                CurrentUserFullName = ReportMemberImpersonated.FullName,
                CurrentUserId = ReportMemberImpersonated.Id,
                IsAdminCurrentUser = ReportMemberImpersonated.User.IsAdmin,
                IsManagerCurrentUser = ReportMemberImpersonated.User.IsManager,
            };
        }

        private List<ReportsSettingsView> GetCustomQueries()
        {
            var customQueries = Uow.ReportsSettingsRepository.LinkedCacheGetByMemberId(ReportMemberImpersonated.Id).Where(x => x.QueryName != null);

            return customQueries.Select(x => x.GetView()).ToList();
        }

        private ReportDropDownsDateStaticExtendView CreateDateStaticExtend(int? dateStaticId)
        {
            var dateStaticInfo = GetDatesStaticInfo().FirstOrDefault(x => x.Id == dateStaticId);
            if (dateStaticInfo != null)
            {
                var dateStaticExtend = new ReportDropDownsDateStaticExtendView
                {
                    DateStatic = GetDatesStaticInfo(),

                    DateFrom = dateStaticInfo.DateFrom,
                    DateTo = dateStaticInfo.DateTo
                };

                return dateStaticExtend;
            }

            throw new CoralTimeDangerException($"Incorrect DateStaticId = { dateStaticId }");
        }

        private ReportDropDownsDateStaticView[] GetDatesStaticInfo()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day).Date;
            var yesterday = today.AddDays(-1);

            var memberDayOfWeekStart = ReportMemberImpersonated.WeekStart == Constants.WeekStart.Monday
                ? DayOfWeek.Monday
                : DayOfWeek.Sunday;

            CommonHelpers.SetRangeOfThisWeekByDate(out var thisWeekStart, out var thisWeekEnd, today, memberDayOfWeekStart);

            CommonHelpers.SetRangeOfThisMonthByDate(out var thisMonthByTodayFirstDate, out var thisMonthByTodayLastDate, today);

            CommonHelpers.SetRangeOfThisYearByDate(out var thisYearByTodayFirstDate, out var thisYearByTodayLastDate, today);

            CommonHelpers.SetRangeOfLastWeekByDate(out var lastWeekStart, out var lastWeekEnd, today, memberDayOfWeekStart);

            CommonHelpers.SetRangeOfLastMonthByDate(out var lastMonthByTodayFirstDate, out var lastMonthByTodayLastDate, today);

            CommonHelpers.SetRangeOfLastYearByDate(out var lastYearByTodayFirstDate, out var lastYearByTodayLastDate, today);

            ReportDropDownsDateStaticView[] datesStaticInfo =
            {
                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.Today,
                    Description = "Today",
                    DateFrom = today,
                    DateTo = today
                },

                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.ThisWeek,
                    Description = "This Week",
                    DateFrom = thisWeekStart.Date,
                    DateTo = thisWeekEnd.Date
                },

                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.ThisMonth,
                    Description = "This Month",
                    DateFrom = thisMonthByTodayFirstDate.Date,
                    DateTo = thisMonthByTodayLastDate.Date
                },

                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.ThisYear,
                    Description = "This Year",
                    DateFrom = thisYearByTodayFirstDate.Date,
                    DateTo = thisYearByTodayLastDate.Date
                },

                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.Yesterday,
                    Description = "Yesterday",
                    DateFrom = yesterday,
                    DateTo = yesterday
                },

                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.LastWeek,
                    Description = "Last Week",
                    DateFrom = lastWeekStart.Date,
                    DateTo = lastWeekEnd.Date
                },

                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.LastMonth,
                    Description = "Last Month",
                    DateFrom = lastMonthByTodayFirstDate.Date,
                    DateTo = lastMonthByTodayLastDate.Date
                },

                new ReportDropDownsDateStaticView
                {
                    Id = (int) Constants.DatesStaticIds.LastYear,
                    Description = "Last Year",
                    DateFrom = lastYearByTodayFirstDate.Date,
                    DateTo = lastYearByTodayLastDate.Date
                }
            };

            return datesStaticInfo;
        }

        private ReportsSettingsView GetCurrentQuery()
        {
            var reportsSettings = Uow.ReportsSettingsRepository.LinkedCacheGetByMemberId(ReportMemberImpersonated.Id).FirstOrDefault(x => x.IsCurrentQuery);

            return reportsSettings?.GetView();
        }

        private ReportsSettingsView CreateQueryWithDefaultValues()
        {
            var reportsSettingsView = new ReportsSettingsView().GetViewWithDefaultValues();

            var dateStaticExtend = CreateDateStaticExtend(reportsSettingsView.DateStaticId);
            reportsSettingsView.DateFrom = dateStaticExtend.DateFrom;
            reportsSettingsView.DateTo = dateStaticExtend.DateTo;

            var reportsSettings = new ReportsSettings().GetModel(reportsSettingsView, ReportMemberImpersonated.Id);

            Uow.ReportsSettingsRepository.Insert(reportsSettings);
            Uow.Save();
            Uow.ReportsSettingsRepository.LinkedCacheClear();

            reportsSettingsView = reportsSettings.GetView();

            return reportsSettingsView;
        }
    }
}