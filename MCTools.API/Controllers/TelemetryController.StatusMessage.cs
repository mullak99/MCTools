using MCTools.SDK.Models.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MCTools.API.Controllers
{
	public partial class TelemetryController
	{
		[HttpGet("statusmessage/getall")]
		[ResponseCache(Duration = 600, Location = ResponseCacheLocation.Client, NoStore = false)]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of status messages")]
		public async Task<IActionResult> GetStatusMessage()
		{
			try
			{
				// TODO: Placeholder
				List<ApiMessage> list = new()
				{ };
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting messages");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
