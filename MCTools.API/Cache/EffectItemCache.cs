using MCTools.SDK.Models;
using Microsoft.Extensions.Caching.Memory;

namespace MCTools.API.Cache
{
	public class EffectItemCache : IEffectItemCache
	{
		private MemoryCache _cache;

		public EffectItemCache()
		{
			_cache = new MemoryCache(new MemoryCacheOptions());
		}

		public void Set(EffectItem item)
			=> _cache.Set(item.Id, item, DateTimeOffset.Now.AddHours(12));

		public void Remove(Guid id)
			=> _cache.Remove(id);

		public void RemoveAll()
			=> _cache = new MemoryCache(new MemoryCacheOptions());

		public bool TryGetValue(Guid id, out EffectItem item)
			=> _cache.TryGetValue(id, out item);

		public int Count => (int)_cache.Count;
	}

	public interface IEffectItemCache
	{
		void Set(EffectItem item);
		void Remove(Guid id);
		void RemoveAll();
		bool TryGetValue(Guid id, out EffectItem item);
	}
}
