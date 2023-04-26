using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MCTools.Models;
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

		public async Task<List<MCVersion>> GetJavaVersions()
		{
			HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/assets/versions");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<List<MCVersion>>(rawJson);
			}
			return new List<MCVersion>();
		}

		public async Task<List<MCVersion>> GetBedrockVersions()
		{
			HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/assets/bedrock/versions");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<List<MCVersion>>(rawJson);
			}
			return new List<MCVersion>();
		}

		public async Task<MCAssets> GetJavaAssets(string version)
		{
			HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/assets/version/{version}");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<MCAssets>(rawJson);
			}
			return new MCAssets();
		}

		public async Task<MCAssets> GetBedrockAssets(string version)
		{
			HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/assets/bedrock/version/{version}");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<MCAssets>(rawJson);
			}
			return new MCAssets();
		}

		public async Task<string> GetJavaJar(string version)
		{
			HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/assets/version/{version}/jar");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
				return await res.Content.ReadAsStringAsync();
			return string.Empty;
		}
	}
}
