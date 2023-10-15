using MCTools.API.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MCTools.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class AdminController : ControllerBase
	{
		private readonly IToolsLogic _toolsLogic;
		private readonly ILogger<AdminController> _logger;

		public AdminController(IToolsLogic toolsLogic, ILogger<AdminController> logger)
		{
			_toolsLogic = toolsLogic;
			_logger = logger;
		}

		[HttpDelete("purge")]
		[SwaggerResponse(200, Description = "Queued asset purging")]
		[Authorize("write:purge-assets")]
		public IActionResult PurgeAssets()
		{
			try
			{
				_ = Task.Run(() => _toolsLogic.PurgeAssets());
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while purging assets");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("purge/all")]
		[SwaggerResponse(200, Description = "Queued asset purging")]
		[Authorize("write:purge-assets")]
		public IActionResult PurgeAllAssets()
		{
			try
			{
				_ = Task.Run(() => _toolsLogic.PurgeAllAssets());
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while purging all assets");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("purge/cache")]
		[SwaggerResponse(200, Description = "Queued cache purging")]
		[Authorize("write:purge-assets")]
		public IActionResult PurgeCache()
		{
			try
			{
				_ = Task.Run(() => _toolsLogic.PurgeCache());
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while purging cache");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
