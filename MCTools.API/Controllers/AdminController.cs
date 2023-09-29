using MCTools.API.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

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

		public AdminController(IToolsLogic toolsLogic)
		{
			_toolsLogic = toolsLogic;
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
				return StatusCode(500, ex.Message);
			}
		}
	}
}
