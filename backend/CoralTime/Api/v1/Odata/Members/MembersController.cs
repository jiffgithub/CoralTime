using AutoMapper;
using CoralTime.BL.Interfaces;
using CoralTime.ViewModels.Member;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static CoralTime.Common.Constants.Constants;
using static CoralTime.Common.Constants.Constants.Routes;
using static CoralTime.Common.Constants.Constants.Routes.OData;

using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using CoralTime.Common.Middlewares;

namespace CoralTime.Api.v1.Odata.Members
{
    [Authorize]
    public class MembersController : BaseODataController<MembersController, IMemberService>
    {
        private readonly IImageService _avatarService;

        public MembersController(IMemberService service, ILogger<MembersController> logger, IMapper mapper, IImageService avatarService)
            : base(logger, mapper, service)
        {
            _avatarService = avatarService;
        }

        // GET: api/v1/odata/Members    x
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                return Ok(_service.GetAllMembers());
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        // GET api/v1/odata/Members(5)
        [HttpGet(MembersWithIdRoute)]
        public IActionResult GetById(int id)
        {
            try
            {
                return Ok(_service.GetById(id));
            }
            catch (Exception e)
            {
              return SendErrorODataResponse(e);
            }
        }

        // GET api/v1/odata/Members(2)/projects
        [HttpGet(MembersRouteWithProjects)]
        public IActionResult GetProjects([FromODataUri]int id)
        {
            try
            {
                return Ok(_service.GetTimeTrackerAllProjects(id));
            }
            catch (Exception e)
            {
               return SendErrorODataResponse(e);
            }
        }

        // POST: api/v1/odata/Members
        [HttpPost(MemberRoute)]
        [Authorize(Roles = ApplicationRoleAdmin)]
        public async Task<IActionResult> Create([FromBody] MemberView memberView)
        {
            if (!ModelState.IsValid)
            {
                //return SendInvalidModelResponse();
            }

            var createdMemberView = await _service.CreateNewUser(memberView, GetBaseUrl());

            var locationUri = $"{Request.Host}/{BaseODataRoute}/Members/{memberView.Id}";

            return base.Created(locationUri, (object)createdMemberView);
        }

        // PUT: api/v1/odata/Members(1)
      
        [HttpPut(MembersWithIdRoute)]
        public async Task<IActionResult> Update(int id, MemberView memberView)
        {
            if (!ModelState.IsValid)
            {
               // return SendInvalidModelResponse();
            }

            memberView.Id = id;
            
            try
            {
                return Ok(await _service.Update(memberView, GetBaseUrl()));
            }
            catch (Exception e)
            {
                return SendErrorODataResponse(e);
            }
        }

        //DELETE :api/v1/odata/Members(1)
        
        [HttpDelete(MembersWithIdRoute)]
        [Authorize(Roles = ApplicationRoleAdmin)]
        public IActionResult Delete([FromODataUri]int id) => BadRequest($"Can't delete the member with Id - {id}");

    }
}