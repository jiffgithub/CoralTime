using CoralTime.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using static CoralTime.Common.Constants.Constants;
using static CoralTime.Common.Constants.Constants.Routes.OData;

namespace CoralTime.Api.v1.Odata.Projects
{

    [Authorize]
    public class ProjectsController : BaseODataController<ProjectsController, IProjectService>
    {
        public ProjectsController(IProjectService service, ILogger<ProjectsController> logger)
            : base(logger, service) { }

        // GET: api/v1/odata/Projects
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                return Ok(_service.TimeTrackerAllProjects());
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // GET api/v1/odata/Projects(2)
        [HttpGet(ProjectsRouteWithMembers)]
        public IActionResult GetMembers([FromODataUri] int id)
        {
            try
            {
                return Ok(_service.GetMembers(id));
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // GET api/v1/odata/Projects(2)
        [ODataRouteComponent(ProjectsWithIdRoute)]
        public IActionResult GetById([FromODataUri]  int id)
        {
            try
            {
                var result = _service.GetById(id);
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // POST api/v1/odata/Projects
        [Authorize(Roles = ApplicationRoleAdmin)]
        [HttpPost(ProjectsRoute)]
        public IActionResult Create([FromBody] dynamic projectData)
        {
            if (!ModelState.IsValid)
            {
                SendInvalidModelResponse();
            }

            try
            {
                var result = _service.Create(projectData);
                var locationUri = $"{Request.Host}/{BaseODataRoute}/Projects({result.Id})";

                return Created(locationUri, result);
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // PUT api/v1/odata/Projects(1)
        [HttpPut(ProjectsWithIdRoute)]
        public IActionResult Update([FromODataUri] int id, [FromBody]dynamic project)
        {
            if (!ModelState.IsValid)
            {
                SendInvalidModelResponse();
            }

            try
            {
                var result = _service.Update(id, project);
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // PATCH api/v1/odata/Projects(1)
        [HttpPatch(ProjectsWithIdRoute)]
        public IActionResult Patch([FromODataUri] int id, [FromBody]dynamic project)
        {
            if (!ModelState.IsValid)
            {
                SendInvalidModelResponse();
            }

            try
            {
                var result = _service.Patch(id, project);
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // DELETE api/v1/odata/Projects(1)
        [Authorize(Roles = ApplicationRoleAdmin)]
        [HttpDelete(ProjectsWithIdRoute)]
        public IActionResult Delete([FromODataUri] int id)
        {
            return BadRequest($"Can't delete the project with Id - {id}");
        }
    }
}