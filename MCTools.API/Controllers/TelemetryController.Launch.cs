using MCTools.SDK.Enums.Telemetry;
using MCTools.SDK.Models.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MCTools.API.Controllers
{
	public partial class TelemetryController
	{
		[HttpPost("launch")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public async Task<IActionResult> AddAppVisit([FromBody] AppInfo appInfo)
		{
			try
			{
				await _telemetryLogic.AddAppVisit(appInfo);
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while adding app visit");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/{from}/{to}")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of applications that were launched between the specified time")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisits([FromRoute] DateTime from, [FromRoute] DateTime to)
		{
			try
			{
				List<AppInfo> list = await _telemetryLogic.GetAppVisits(from, to);
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/of_type/{releaseType}/{from}/{to}")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of applications that were launched with a specific release type and between the specified time")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisits([FromRoute] AppReleaseType releaseType, [FromRoute] DateTime from, [FromRoute] DateTime to)
		{
			try
			{
				List<AppInfo> list = await _telemetryLogic.GetAppVisits(releaseType, from, to);
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/of_type/{releaseType}/total")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of applications that were launched with a specific release type")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisits([FromRoute] AppReleaseType releaseType)
		{
			try
			{
				List<AppInfo> list = await _telemetryLogic.GetAppVisits(releaseType);
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/total")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of applications that were launched")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisits()
		{
			try
			{
				List<AppInfo> list = await _telemetryLogic.GetAppVisits();
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/count/{from}/{to}")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(long), Description = "A count of applications that were launched between the specified time")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisitsCount([FromRoute] DateTime from, [FromRoute] DateTime to)
		{
			try
			{
				long count = await _telemetryLogic.GetAppVisitsCount(from, to);
				return Ok(count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits count");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/count/of_type/{releaseType}/{from}/{to}")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(long), Description = "A count of applications that were launched with a specific release type and between the specified time")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisitsCount([FromRoute] AppReleaseType releaseType, [FromRoute] DateTime from, [FromRoute] DateTime to)
		{
			try
			{
				long count = await _telemetryLogic.GetAppVisitsCount(releaseType, from, to);
				return Ok(count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits count");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/count/of_type/{releaseType}/total")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(long), Description = "A count of applications that were launched with a specific release type")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisitsCount([FromRoute] AppReleaseType releaseType)
		{
			try
			{
				long count = await _telemetryLogic.GetAppVisitsCount(releaseType);
				return Ok(count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits count");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("launch/get/count/total")]
		[Authorize("read:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(long), Description = "A count of applications that were launched")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> GetAppVisitsCount()
		{
			try
			{
				long count = await _telemetryLogic.GetAppVisitsCount();
				return Ok(count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting app visits count");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("launch/purge")]
		[Authorize("delete:telemetry")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[SwaggerResponse(200, Type = typeof(long), Description = "A count of entries deleted")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> PurgeAppVisits()
		{
			try
			{
				long count = await _telemetryLogic.PurgeAppVisits();
				return Ok(count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while purging app visits");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
