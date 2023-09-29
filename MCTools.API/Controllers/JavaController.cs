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
	public class JavaController : ControllerBase
	{
		private readonly IToolsLogic _toolsLogic;
		private const int GET_CACHE_DURATION = 1800; // 30 minutes

		public JavaController(IToolsLogic toolsLogic)
		{
			_toolsLogic = toolsLogic;
		}

		[HttpGet("versions")]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AssetMCVersion>), Description = "A list of all supported Java versions")]
		[SwaggerResponse(400, "No versions could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new []{ "bypassVersionLimit" }, NoStore = false)]
		public async Task<IActionResult> GetAllVersions([FromQuery] bool bypassVersionLimit = false)
		{
			try
			{
				var versions = await _toolsLogic.GetJavaMCVersions(bypassVersionLimit);

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
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Assets for a specified Java version")]
		[SwaggerResponse(400, "No assets could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION, VaryByQueryKeys = new[] { "mcVersion" }, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetVersionAssets([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftJavaAssets(mcVersion);

				if (assets.IsSuccess)
					return Ok(assets.Data);
				return BadRequest(assets.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("version/{mcVersion}/jar")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Jar for a specified Java version")]
		[SwaggerResponse(400, "Invalid version")]
		[ResponseCache(Duration = GET_CACHE_DURATION, VaryByQueryKeys = new[] { "mcVersion" }, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetMinecraftJarUrl([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftJavaJar(mcVersion);

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
		[SwaggerResponse(200, Description = "Assets for all supported Java versions have been pre-generated")]
		[Authorize("write:pregenerate-assets")]
		public async Task<IActionResult> PregenerateAssets([FromQuery] bool bypassVersionLimit = false)
		{
			try
			{
				return Ok(await _toolsLogic.PregenerateJavaAssets(bypassHighestVersionLimit: bypassVersionLimit));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}
	}
}
