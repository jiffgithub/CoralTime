using CoralTime.BL.Interfaces.Reports;
using CoralTime.ViewModels.Reports.Request.Grid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static CoralTime.Common.Constants.Constants.Routes;

namespace CoralTime.Api.v1.Reports
{
    [Authorize]
    [Route(BaseControllerRoute)]
    public class ReportsController : BaseController<ReportsController, IReportsService>
    {
        public ReportsController(IReportsService service, ILogger<ReportsController> logger)
            : base(logger, service) { }

        [HttpGet]
        public IActionResult GetReportsDropdowns() => new JsonResult(_service.GetReportsDropDowns());

        [HttpPost]
        public IActionResult GetReportsGridAndSaveCurrentQuery([FromBody] ReportsGridView reportsGridView)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid Model");
            }

            var jsonSerializatorSettings = new JsonSerializerOptions
            {
            };
            
            jsonSerializatorSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonSerializatorSettings.IgnoreNullValues = true;

            _service.CheckAndSaveCurrentQuery(reportsGridView);

            var reportsGrid = _service.GetReportsGrid(reportsGridView);

            return new JsonResult(reportsGrid, jsonSerializatorSettings);
        }
    }
}
