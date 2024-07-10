using MCTools.SDK.Models.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MCTools.API.Controllers
{
	public partial class TelemetryController
	{
		[HttpGet("statusmessage/getall")]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AppInfo>), Description = "A list of status messages")]
		[ResponseCache(Duration = 600, Location = ResponseCacheLocation.Client, NoStore = false)]
		public async Task<IActionResult> GetStatusMessages()
		{
			try
			{
				// TODO: Placeholder
				List<ApiMessage> list = new();

				await Task.CompletedTask;
				return Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting messages");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPost("statusmessage/add")]
		[SwaggerResponse(200, Description = "If the message was added successfully")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("write:statusmessage")]
		public async Task<IActionResult> AddStatusMessage([FromBody] IApiMessage message)
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while adding a message");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPost("statusmessage/edit/{id}")]
		[SwaggerResponse(200, Description = "If the message was added successfully")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("write:statusmessage")]
		public async Task<IActionResult> EditStatusMessage([FromRoute] Guid id, [FromBody] IApiMessage message)
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while editing a message");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("statusmessage/deleteall")]
		[SwaggerResponse(200, Description = "If the messages were deleted successfully")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("delete:statusmessage")]
		public async Task<IActionResult> DeleteStatusMessages()
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while deleting the message");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("statusmessage/delete/{id}")]
		[SwaggerResponse(200, Description = "If the message was deleted successfully")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("delete:statusmessage")]
		public async Task<IActionResult> DeleteStatusMessage([FromRoute] Guid id)
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while deleting the message");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
