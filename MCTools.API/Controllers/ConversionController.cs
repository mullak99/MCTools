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
				var result = await _conversionLogic.GetAllEffectItems();
				return Ok(result);
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
		public async Task<IActionResult> GetItemByName([FromRoute] string name)
		{
			try
			{
				string itemNameSanitized = name.Replace("_", " ");
				var result = await _conversionLogic.GetEffectItem(itemNameSanitized);

				if (result == null)
					return BadRequest($"No effect item with the name {itemNameSanitized} could be found");
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting the effect item");
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
				var result = await _conversionLogic.GetEffectItem(id);

				if (result == null)
					return BadRequest($"No effect item with the id {id} could be found");
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while getting the effect item");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPost("effectitem/add")]
		[SwaggerResponse(200, Type = typeof(bool), Description = "If the effect item was added successfully")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("write:effectitem")]
		public async Task<IActionResult> AddItem([FromBody] EffectItem item)
		{
			try
			{
				var result = await _conversionLogic.AddEffectItem(item);
				return result ? Ok() : BadRequest("Invalid effect item!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while adding the effect item");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPost("effectitem/addmany")]
		[SwaggerResponse(200, Type = typeof(bool), Description = "If the effect item was added successfully")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("write:effectitem")]
		public async Task<IActionResult> AddItems([FromBody] List<EffectItem> items)
		{
			try
			{
				var result = await _conversionLogic.AddEffectItems(items);
				return result ? Ok() : BadRequest("Invalid effect items!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while adding the effect items");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPatch("effectitem/edit/{id}")]
		[SwaggerResponse(200, Type = typeof(EffectItem), Description = "If the effect item was edited successfully")]
		[SwaggerResponse(400, "No effect item could be found")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("write:effectitem")]
		public async Task<IActionResult> EditItemById([FromRoute] Guid id, [FromBody] EffectItem item)
		{
			try
			{
				var result = await _conversionLogic.UpdateEffectItem(id, item);
				return result ? Ok() : BadRequest("Invalid request!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while editing the effect item");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("effectitem/delete/{id}")]
		[SwaggerResponse(200, Type = typeof(bool), Description = "If the effect item was deleted successfully")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("delete:effectitem")]
		public async Task<IActionResult> DeleteItemById([FromRoute] Guid id)
		{
			try
			{
				var result = await _conversionLogic.DeleteEffectItem(id);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while deleting the effect item");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("effectitem/deleteall")]
		[SwaggerResponse(200, Type = typeof(int), Description = "Number of effect items deleted")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[Authorize("delete:effectitem")]
		public async Task<IActionResult> DeleteItems()
		{
			try
			{
				var result = await _conversionLogic.DeleteAllEffectItems();
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while deleting the effect items");
				return StatusCode(500, ex.Message);
			}
		}
	}
}
