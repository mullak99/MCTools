using MCTools.SDK.Interfaces.Controllers;
using MCTools.SDK.Models;
using Newtonsoft.Json;

namespace MCTools.SDK.Controllers
{
    public class JavaController : IEditionController
	{
		private IApiClient _client { get; }
		private string _apiVersion { get; }

		public JavaController(IApiClient client, string apiVersion = "1.0")
		{
			_client = client;
			_apiVersion = apiVersion;
		}

		public async Task<List<MCVersion>> GetVersions()
			=> await GetVersions(false);

		public async Task<List<MCVersion>> GetVersions(bool bypassVersionLimit)
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri($"java/versions?bypassVersionLimit={bypassVersionLimit}", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<MCVersion>>(rawJson) ?? new();
		}

		public async Task<MCAssets> GetAssets(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri($"java/version/{version}", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<MCAssets>(rawJson) ?? new();
		}

		public async Task<string> GetJar(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri($"java/version/{version}/jar", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
				return await res.Content.ReadAsStringAsync();
			return string.Empty;
		}

		public async Task<bool> GetOverlaySupport(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri($"java/version/{version}/supports-overlays", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return true;

			return !bool.TryParse(await res.Content.ReadAsStringAsync(), out bool result) || result;
		}
	}
}
