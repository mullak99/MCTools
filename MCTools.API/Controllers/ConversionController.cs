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
	public class ConversionController : ControllerBase
	{
		private readonly IConversionLogic _conversionLogic;
		private readonly ILogger<ConversionController> _logger;
		private const int GET_CACHE_DURATION_ALL = 1800; // 30 minutes

		public ConversionController(IConversionLogic conversionLogic, ILogger<ConversionController> logger)
		{
			_conversionLogic = conversionLogic;
			_logger = logger;
		}

		[HttpGet("effectitems")]
		[SwaggerResponse(200, Type = typeof(IEnumerable<EffectItem>), Description = "A list of all effect items (potions and tipped arrows), including their colours and needed variations for Bedrock")]
		[SwaggerResponse(400, "No effect items could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION_ALL, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetAllItems()
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting all effect items");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("effectitem/get/byname/{name}")]
		[SwaggerResponse(200, Type = typeof(EffectItem), Description = "An effect item, including its colour and needed variations for Bedrock")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION_ALL, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetItemByName([FromRoute] string itemName)
		{
			try
			{
				string itemNameSanitized = itemName.Replace("_", " ");

				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting the effect items");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("effectitem/get/byid/{id}")]
		[SwaggerResponse(200, Type = typeof(EffectItem), Description = "An effect item, including its colour and needed variations for Bedrock")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = GET_CACHE_DURATION_ALL, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetItemById([FromRoute] Guid id)
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting the effect items");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPost("effectitem/add/{id}")]
		[SwaggerResponse(200, Type = typeof(EffectItem), Description = "An effect item, including its colour and needed variations for Bedrock")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("write:effectitem")]
		public async Task<IActionResult> AddItem([FromRoute] Guid id, [FromBody] EffectItem item)
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while adding the effect items");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPatch("effectitem/edit/{id}")]
		[SwaggerResponse(200, Type = typeof(EffectItem), Description = "An effect item, including its colour and needed variations for Bedrock")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("write:effectitem")]
		public async Task<IActionResult> EditItemById([FromRoute] Guid id, [FromBody] EffectItem item)
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while editing the effect items");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("effectitem/delete/{id}")]
		[SwaggerResponse(200, Type = typeof(EffectItem), Description = "An effect item, including its colour and needed variations for Bedrock")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("delete:effectitem")]
		public async Task<IActionResult> DeleteItemById([FromRoute] Guid id)
		{
			try
			{
				await Task.CompletedTask;
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while deleting the effect items");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
