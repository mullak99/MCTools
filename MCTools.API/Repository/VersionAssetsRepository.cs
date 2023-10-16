using MCTools.API.Cache;
using MCTools.SDK.Models;
using MongoDB.Driver;

namespace MCTools.API.Repository
{
	public class VersionAssetsRepository : IVersionAssetsRepository
	{
		private readonly IMongoCollection<MinecraftVersionAssets> _collection;
		private readonly IVersionAssetsCache _cache;

		public VersionAssetsRepository(IMongoDatabase database, IVersionAssetsCache cache, GlobalSettings globalSettings)
		{
			string dbName = "MinecraftVersionAssets";
			if (!string.IsNullOrWhiteSpace(globalSettings.DbNameSuffix))
				dbName += $"-{globalSettings.DbNameSuffix}";
			_collection = database.GetCollection<MinecraftVersionAssets>(dbName);
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

		public async Task<long> DeleteAllVersionAssets()
		{
			try
			{
				var versions = await _collection.Find(_ => true).ToListAsync();
				var result = await _collection.DeleteManyAsync(_ => true);

				if (result.DeletedCount > 0)
				{
					foreach (var version in versions)
						_cache.Remove(version.Name, version.Edition, version.Version);
				}

				return result.DeletedCount;
			}
			catch (Exception)
			{
				return 0;
			}
		}

		public async Task<long> DeleteOldVersionAssets(int currentVersion)
		{
			try
			{
				var versions = await _collection.Find(x => x.Version < currentVersion).ToListAsync();
				var result = await _collection.DeleteManyAsync(x => x.Version < currentVersion);

				if (result.DeletedCount > 0)
				{
					foreach (var version in versions)
						_cache.Remove(version.Name, version.Edition, version.Version);
				}

				return result.DeletedCount;
			}
			catch (Exception)
			{
				return 0;
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

	public interface IVersionAssetsRepository
	{
		// Create
		Task AddVersionAssets(MinecraftVersionAssets assets);

		// Read
		Task<MinecraftVersionAssets?> GetVersionAssets(string name, string edition, int assetVersion);
		Task<List<MinecraftVersionAssets>> GetAllVersionAssets();

		// Delete
		Task<bool> DeleteVersionAssets(string name, string edition, int assetVersion);
		Task<long> DeleteAllVersionAssets();
		Task<long> DeleteOldVersionAssets(int currentVersion);
		bool DeleteCache();
	}
}
