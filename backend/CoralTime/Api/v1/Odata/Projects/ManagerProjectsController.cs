using CoralTime.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

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
                return Ok(_service.ManageProjectsOfManager());
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }
    }
}