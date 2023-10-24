using MCTools.API.Cache;
using MCTools.SDK.Models;
using MongoDB.Driver;

namespace MCTools.API.Repository
{
	public class EffectItemRepository : IEffectItemRepository
	{
		private readonly IMongoCollection<EffectItem> _collection;
		private readonly IEffectItemCache _cache;
		private readonly ILogger<EffectItemRepository> _logger;

		public EffectItemRepository(IMongoDatabase database, IEffectItemCache cache, ILogger<EffectItemRepository> logger, GlobalSettings globalSettings)
		{
			string dbName = "EffectItem";
			if (!string.IsNullOrWhiteSpace(globalSettings.DbNameSuffix))
				dbName += $"-{globalSettings.DbNameSuffix}";
			_collection = database.GetCollection<EffectItem>(dbName);
			_cache = cache;
			_logger = logger;
		}

		#region Create

		public async Task<bool> AddEffectItem(EffectItem item)
		{
			try
			{
				await _collection.InsertOneAsync(item);
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
				await _collection.InsertManyAsync(items);
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while adding effect items: {e}");
				return false;
			}
		}
		#endregion

		#region Read

		public async Task<List<EffectItem>> GetAllEffectItems()
		{
			try
			{
				return await _collection.Find(_ => true).ToListAsync();
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
				if (_cache.TryGetValue(id, out EffectItem item))
					return item;

				return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
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
				return await _collection.Find(x =>
						x.Name != null &&
						x.PotionName != null &&
						(string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase) || string.Equals(x.PotionName, name, StringComparison.CurrentCultureIgnoreCase)))
					.FirstOrDefaultAsync();
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while getting effect item with name {name}: {e}");
				return null;
			}
		}
		#endregion

		#region Update
		public async Task<bool> UpdateEffectItem(Guid id, EffectItem editedItem)
		{
			try
			{
				// Set the id to the edited item
				editedItem.Id = id;

				// Update the item in the database
				var filterBuilder = Builders<EffectItem>.Filter;
				var filter = filterBuilder.Eq(t => t.Id, id);
				var result = await _collection.ReplaceOneAsync(filter, editedItem);

				bool updated = result.ModifiedCount > 0;
				if (updated)
					_cache.Remove(id);

				return updated;
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while updating effect item with id {id}: {e}");
				throw;
			}
		}
		#endregion

		#region Delete
		public async Task<int> DeleteAllEffectItems()
		{
			try
			{
				var deleted = await _collection.DeleteManyAsync(_ => true);
				int deletedCount = (int)deleted.DeletedCount;

				if (deletedCount > 0)
					_cache.RemoveAll();

				return deletedCount;
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while deleting all effect items: {e}");
				return 0;
			}
		}

		public async Task<bool> DeleteEffectItem(Guid id)
		{
			try
			{
				var filterBuilder = Builders<EffectItem>.Filter;
				var filter = filterBuilder.Eq(t => t.Id, id);
				var deleted = await _collection.DeleteOneAsync(filter);

				bool updated = deleted.DeletedCount > 0;
				if (updated)
					_cache.Remove(id);

				return updated;
			}
			catch (Exception e)
			{
				_logger.LogError($"An error occurred while deleting effect item with id {id}: {e}");
				return false;
			}
		}

		public bool DeleteCache()
		{
			try
			{
				_cache.RemoveAll();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}

	public interface IEffectItemRepository
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
		bool DeleteCache();
		#endregion
	}
}
