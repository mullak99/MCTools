using MCTools.API.Logic;
using MCTools.SDK.Models;
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
	public class BedrockController : ControllerBase
	{
		private readonly IToolsLogic _toolsLogic;
		private const int GET_CACHE_DURATION = 1800; // 30 minutes

		public BedrockController(IToolsLogic toolsLogic)
		{
			_toolsLogic = toolsLogic;
		}

		[HttpGet("versions")]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AssetMCVersion>), Description = "A list of all supported Bedrock versions")]
		[SwaggerResponse(400, "No versions could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetAllVersions()
		{
			try
			{
				var versions = await _toolsLogic.GetBedrockMCVersions();

				if (versions.Any())
					return Ok(versions);
				return BadRequest("No versions could be found!");
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("version/{mcVersion}")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Assets for a specified Bedrock version")]
		[SwaggerResponse(400, "No assets could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION, VaryByQueryKeys = new[] { "mcVersion" }, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetVersionAssets([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftBedrockAssets(mcVersion);

				if (assets.IsSuccess)
					return Ok(assets.Data);
				return BadRequest(assets.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("pregenerate")]
		[SwaggerResponse(200, Description = "Assets for all supported Bedrock versions have been pre-generated")]
		[Authorize("write:pregenerate-assets")]
		public async Task<IActionResult> PregenerateAssets()
		{
			try
			{
				return Ok(await _toolsLogic.PregenerateBedrockAssets());
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}
	}
}
