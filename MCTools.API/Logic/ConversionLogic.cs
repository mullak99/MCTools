using MCTools.API.Repository;
using MCTools.SDK.Models;

namespace MCTools.API.Logic
{
	public class ConversionLogic : IConversionLogic
	{
		private readonly IEffectItemRepository _effectItemRepository;
		private readonly ILogger<ConversionLogic> _logger;

		public ConversionLogic(IEffectItemRepository effectItemRepository, ILogger<ConversionLogic> logger)
		{
			_effectItemRepository = effectItemRepository;
			_logger = logger;
		}

		public async Task<bool> AddEffectItem(EffectItem item)
		{
			try
			{
				await _effectItemRepository.AddEffectItem(item);
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while adding effect item {item.Name}: {e}");
				return false;
			}
		}

		public async Task<bool> AddEffectItems(List<EffectItem> items)
		{
			try
			{
				await _effectItemRepository.AddEffectItems(items);
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while adding {items.Count} effect items: {e}");
				return false;
			}
		}

		public async Task<List<EffectItem>> GetAllEffectItems()
		{
			try
			{
				return await _effectItemRepository.GetAllEffectItems();
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while getting all effect items: {e}");
				return new List<EffectItem>();
			}
		}

		public async Task<EffectItem?> GetEffectItem(Guid id)
		{
			try
			{
				return await _effectItemRepository.GetEffectItem(id);
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while getting effect item with id {id}: {e}");
				return null;
			}
		}

		public async Task<EffectItem?> GetEffectItem(string name)
		{
			try
			{
				return await _effectItemRepository.GetEffectItem(name);
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while getting effect item with name {name}: {e}");
				return null;
			}
		}

		public async Task<bool> UpdateEffectItem(Guid id, EffectItem item)
		{
			try
			{
				return await _effectItemRepository.UpdateEffectItem(id, item);
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while updating effect item with id {id}: {e}");
				return false;
			}
		}

		public async Task<bool> DeleteEffectItem(Guid id)
		{
			try
			{
				return await _effectItemRepository.DeleteEffectItem(id);
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while deleting effect item with id {id}: {e}");
				return false;
			}
		}

		public async Task<int> DeleteAllEffectItems()
		{
			try
			{
				return await _effectItemRepository.DeleteAllEffectItems();
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while deleting all effect items: {e}");
				return 0;
			}
		}
	}

	public interface IConversionLogic
	{
		#region Create
		Task<bool> AddEffectItem(EffectItem item);
		Task<bool> AddEffectItems(List<EffectItem> items);
		#endregion

		#region Read
		Task<List<EffectItem>> GetAllEffectItems();
		Task<EffectItem?> GetEffectItem(Guid id);
		Task<EffectItem?> GetEffectItem(string name);
		#endregion

		#region Update
		Task<bool> UpdateEffectItem(Guid id, EffectItem editedItem);
		#endregion

		#region Delete
		Task<int> DeleteAllEffectItems();
		Task<bool> DeleteEffectItem(Guid id);
		#endregion
	}
}
