using MCTools.SDK.Models.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MCTools.API.Controllers
{
	public partial class TelemetryController
	{
		[HttpPost("action/add/{sessionId}")]
		[SwaggerResponse(200)]
		[SwaggerResponse(400, "Could not find sessionId")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public async Task<IActionResult> AddAppAction([FromRoute] Guid sessionId, [FromBody] AppAction appAction)
		{
			try
			{
				bool added = await _telemetryLogic.AddAppAction(sessionId, appAction);
				if (!added)
					return BadRequest("Could not find sessionId");
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while adding app visit");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("action/get/{sessionId}")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of actions performed by a specific sessionId")]
		[SwaggerResponse(400, "No actions found")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppActions([FromRoute] Guid sessionId)
		{
			try
			{
				List<AppAction>? list = await _telemetryLogic.GetAppActions(sessionId);
				if (list == null)
					return BadRequest("No actions found");
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
