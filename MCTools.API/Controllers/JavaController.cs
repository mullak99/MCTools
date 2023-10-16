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
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class JavaController : ControllerBase
	{
		private readonly IToolsLogic _toolsLogic;
		private readonly ILogger<JavaController> _logger;
		private const int GET_CACHE_DURATION_ALL = 1800; // 30 minutes
		private const int GET_CACHE_DURATION_ASSET = 7200; // 2 hours

		public JavaController(IToolsLogic toolsLogic, ILogger<JavaController> logger)
		{
			_toolsLogic = toolsLogic;
			_logger = logger;
		}

		[HttpGet("versions")]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AssetMCVersion>), Description = "A list of all supported Java versions")]
		[SwaggerResponse(400, "No versions could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION_ALL, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new []{ "bypassVersionLimit" }, NoStore = false)]
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
				_logger.LogError(ex, "An error occurred while getting all Java versions");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("version/{mcVersion}")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Assets for a specified Java version")]
		[SwaggerResponse(400, "No assets could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION_ASSET, VaryByQueryKeys = new[] { "mcVersion" }, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetVersionAssets([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftJavaAssets(mcVersion, true);

				if (assets.IsSuccess)
					return Ok(assets.Data);
				return BadRequest(assets.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"An error occurred while getting Java assets for version {mcVersion}");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("version/{mcVersion}/jar")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Jar for a specified Java version")]
		[SwaggerResponse(400, "Invalid version")]
		[ResponseCache(Duration = GET_CACHE_DURATION_ASSET, VaryByQueryKeys = new[] { "mcVersion" }, Location = ResponseCacheLocation.Any, NoStore = false)]
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
				_logger.LogError(ex, $"An error occurred while getting Java jar for version {mcVersion}");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("version/{mcVersion}/supports-overlays")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Whether a specified Java version supports overlays")]
		[SwaggerResponse(400, "Invalid version")]
		[ResponseCache(Duration = GET_CACHE_DURATION_ASSET, VaryByQueryKeys = new[] { "mcVersion" }, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetOverlaySupport([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftJavaAssets(mcVersion, true);

				if (assets.IsSuccess)
					return Ok(assets.Data?.OverlaySupport ?? false);
				return BadRequest(assets.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"An error occurred while getting Java overlay support for version {mcVersion}");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("pregenerate")]
		[Authorize("write:pregenerate-assets")]
		[SwaggerResponse(200, Description = "Assets for all supported Java versions have been pre-generated")]
		[SwaggerResponse(401, "You are not authorized to access this")]
		public async Task<IActionResult> PregenerateAssets([FromQuery] bool bypassVersionLimit = false)
		{
			try
			{
				return Ok(await _toolsLogic.PregenerateJavaAssets(bypassHighestVersionLimit: bypassVersionLimit));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while pre-generating Java assets");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
