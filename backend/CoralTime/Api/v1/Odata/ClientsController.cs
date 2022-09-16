using CoralTime.BL.Interfaces;
using CoralTime.ViewModels.Clients;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using static CoralTime.Common.Constants.Constants;
using static CoralTime.Common.Constants.Constants.Routes;
using static CoralTime.Common.Constants.Constants.Routes.OData;
using Microsoft.AspNetCore.OData.Formatter;

namespace CoralTime.Api.v1.Odata
{
    
    [Authorize]
    public class ClientsController : BaseODataController<ClientsController, IClientService>
    {
        public ClientsController(IClientService service, ILogger<ClientsController> logger)
            : base(logger, service) { }

        // GET: api/v1/odata/Clients
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                return Ok(_service.GetAllClients());
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // POST: api/v1/odata/Clients
        [HttpPost(ClientsRoute)]
        [Authorize(Roles = ApplicationRoleAdmin)]
        public IActionResult Create([FromBody] ClientView clientData)
        {
            try
            {
                var result = _service.Create(clientData);

                var locationUri = $"{Request.Host}/{BaseODataRoute}/Clients({result.Id})";
                return Created(locationUri, result);
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // GET api/v1/odata/Clients(2)
        [HttpGet(ClientsWithIdRoute)]
        public IActionResult GetById([FromODataUri] int id)
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

        // PUT: api/v1/odata/Clients(2)
        [HttpPut(ClientsWithIdRoute)]
        [Authorize(Roles = ApplicationRoleAdmin)]
        public IActionResult Update([FromODataUri]int id, [FromBody]dynamic clientData)
        {
            try
            {
                var result = _service.Update(id, clientData);
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // PATCH: api/v1/odata/Clients(30)
        [HttpPatch(ClientsWithIdRoute)]
        [Authorize(Roles = ApplicationRoleAdmin)]
        public IActionResult Patch([FromODataUri]int id, [FromBody]dynamic clientData)
        {
            try
            {
                var result = _service.Update(id, clientData);
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        //DELETE :api/v1/odata/Clients(1)
        [HttpDelete(ClientsWithIdRoute)]
        [Authorize(Roles = ApplicationRoleAdmin)]
        public IActionResult Delete([FromODataUri]int id)
        {
            return BadRequest($"Can't delete the client with Id - {id}");
        }
    }
}