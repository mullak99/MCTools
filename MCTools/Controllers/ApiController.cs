using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MCTools.SDK.Models;
using Newtonsoft.Json;

namespace MCTools.Controllers
{
	public class ApiController
	{
		private HttpClient _client { get; }

		public ApiController(HttpClient client)
		{
			_client = client;
		}

		public async Task<List<MCVersion>> GetJavaVersions(bool bypassVersionLimit = false)
		{
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/java/versions?bypassVersionLimit={bypassVersionLimit}");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<List<MCVersion>>(rawJson);
			}
			return new();
		}

		public async Task<List<MCVersion>> GetBedrockVersions()
		{
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/bedrock/versions");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<List<MCVersion>>(rawJson);
			}
			return new();
		}

		public async Task<MCAssets> GetJavaAssets(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/java/version/{version}");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<MCAssets>(rawJson);
			}
			return new();
		}

		public async Task<MCAssets> GetBedrockAssets(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/bedrock/version/{version}");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<MCAssets>(rawJson);
			}
			return new();
		}

		public async Task<string> GetJavaJar(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/java/version/{version}/jar");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
				return await res.Content.ReadAsStringAsync();
			return string.Empty;
		}

		public async Task<bool> GetOverlaySupport(string version)
		{
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/java/version/{version}/supports-overlays");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				if (bool.TryParse(await res.Content.ReadAsStringAsync(), out bool result))
					return result;
			}
			return true;
		}
	}
}
