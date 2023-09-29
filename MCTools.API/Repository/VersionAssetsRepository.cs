using MCTools.API.Cache;
using MCTools.SDK.Models;
using MongoDB.Driver;

namespace MCTools.API.Repository
{
	public class VersionAssetsRepository : IVersionAssetsRepository
	{
		private readonly IMongoCollection<MinecraftVersionAssets> _collection;
		private readonly IVersionAssetsCache _cache;

		public VersionAssetsRepository(IMongoDatabase database, IVersionAssetsCache cache)
		{
			_collection = database.GetCollection<MinecraftVersionAssets>("MinecraftVersionAssets");
			_cache = cache;
		}

		#region Create
		public async Task AddVersionAssets(MinecraftVersionAssets assets)
			=> await _collection.InsertOneAsync(assets);
		#endregion

		#region Read
		public async Task<MinecraftVersionAssets?> GetVersionAssets(string name, string edition, int assetVersion)
		{
			try
			{
				if (_cache.TryGetValue(name, edition, assetVersion, out MinecraftVersionAssets assets))
					return assets;

				var filterBuilder = Builders<MinecraftVersionAssets>.Filter;
				var filter = filterBuilder.And(
					filterBuilder.Eq(t => t.Name, name),
					filterBuilder.Eq(t => t.Edition, edition),
					filterBuilder.Eq(t => t.Version, assetVersion)
				);

				var result = await _collection.Find(filter).FirstOrDefaultAsync();

				if (result != null)
					_cache.Set(name, edition, assetVersion, result);
				return result;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<List<MinecraftVersionAssets>> GetAllVersionAssets()
			=> await _collection.Find(_ => true).ToListAsync();
		#endregion

		#region Delete
		public async Task<bool> DeleteVersionAssets(string name, string edition, int assetVersion)
		{
			try
			{
				var filterBuilder = Builders<MinecraftVersionAssets>.Filter;
				var filter = filterBuilder.And(
					filterBuilder.Eq(t => t.Name, name),
					filterBuilder.Eq(t => t.Edition, edition),
					filterBuilder.Eq(t => t.Version, assetVersion)
				);
				var deleted = await _collection.DeleteOneAsync(filter);
				if (deleted.DeletedCount > 0)
					_cache.Remove(name, edition, assetVersion);

				return deleted.DeletedCount > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}

	public interface IVersionAssetsRepository
	{
		// Create
		Task AddVersionAssets(MinecraftVersionAssets assets);
		Task<List<MinecraftVersionAssets>> GetAllVersionAssets();

		// Read
		Task<MinecraftVersionAssets?> GetVersionAssets(string name, string edition, int assetVersion);

		// Delete
		Task<bool> DeleteVersionAssets(string name, string edition, int assetVersion);
	}
}
