using MCTools.SDK.Models.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MCTools.API.Controllers
{
	public partial class TelemetryController
	{
		[HttpPost("error/add")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public async Task<IActionResult> AddAppError([FromBody] AppError appError)
		{
			try
			{
				await _telemetryLogic.AddAppError(appError);
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while submitting the application error!");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("errors/get/total")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of app errors")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppErrors()
		{
			try
			{
				List<AppError> list = await _telemetryLogic.GetAppErrors();
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app errors");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("errors/get/count/total")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(long), Description = "A count of app errors")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppErrorCount()
		{
			try
			{
				long count = await _telemetryLogic.GetAppErrorCount();
				return Ok(count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app error count");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("errors/purge")]
		[Authorize("delete:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(long), Description = "A count of entries deleted")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> PurgeAppErrors()
		{
			try
			{
				long count = await _telemetryLogic.PurgeAppErrors();
				return Ok(count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while purging app errors");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
