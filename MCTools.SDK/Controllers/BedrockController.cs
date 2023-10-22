using MCTools.SDK.Interfaces.Controllers;
using MCTools.SDK.Models;
using Newtonsoft.Json;

namespace MCTools.SDK.Controllers
{
	public class BedrockController : IEditionController
	{
		private IApiClient _client { get; }
		private string _apiVersion { get; }

		public BedrockController(IApiClient client, string apiVersion = "1.0")
		{
			_client = client;
			_apiVersion = apiVersion;
		}

		public async Task<List<MCVersion>> GetVersions()
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri("bedrock/versions", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<MCVersion>>(rawJson) ?? new();
		}

		public async Task<MCAssets> GetAssets(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri($"bedrock/version/{version}", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<MCAssets>(rawJson) ?? new();
		}
	}
}
