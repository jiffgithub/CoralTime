using CoralTime.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using static CoralTime.Common.Constants.Constants.Routes.OData;

namespace CoralTime.Api.v1.Odata.Projects
{
    
    [Authorize]
    public class ManagerProjectsController : BaseODataController<ManagerProjectsController, IProjectService>
    {
        public ManagerProjectsController(ILogger<ManagerProjectsController> logger, IProjectService service)
            : base(logger, service) { }

        // GET api/v1/odata/ManagerProjects
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var projects = _service.ManageProjectsOfManager().Select(_mapper.Map<ProjectView, ManagerProjectsView>);

                return Ok(projects.ToList());
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }
    }
}