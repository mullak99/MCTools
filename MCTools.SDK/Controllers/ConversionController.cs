using MCTools.SDK.Interfaces.Controllers;
using MCTools.SDK.Models;
using Newtonsoft.Json;

namespace MCTools.SDK.Controllers
{
    public class ConversionController : IController
	{
		private IApiClient _client { get; }
		private string _apiVersion { get; }

		public ConversionController(IApiClient client, string apiVersion = "1.0")
		{
			_client = client;
			_apiVersion = apiVersion;
		}

		public async Task<List<EffectItem>> GetAllEffectItems()
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri("conversion/effectitems", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<EffectItem>>(rawJson) ?? new();
		}

		public async Task<EffectItem> GetEffectItem(Guid id)
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri($"conversion/effectitem/get/byid/{id}", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<EffectItem>(rawJson) ?? new();
		}

		public async Task<EffectItem> GetEffectItem(string name)
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri($"conversion/effectitem/get/byname/{name}", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<EffectItem>(rawJson) ?? new();
		}
	}
}
