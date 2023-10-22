using System.Text;
using MCTools.SDK.Interfaces.Controllers;
using MCTools.SDK.Models.Telemetry;
using Newtonsoft.Json;

namespace MCTools.SDK.Controllers
{
	public class TelemetryController : IController
	{
		private IApiClient _client { get; }
		private string _apiVersion { get; }

		public TelemetryController(IApiClient client, string apiVersion = "1.0")
		{
			_client = client;
			_apiVersion = apiVersion;
		}

		public async Task AddAppLaunch(AppInfo appInfo)
		{
			HttpRequestMessage req = new(HttpMethod.Post, _client.BuildRequestUri("telemetry/launch", _apiVersion));
			req.Content = new StringContent(JsonConvert.SerializeObject(appInfo), Encoding.UTF8, "application/json");
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				Console.WriteLine($"Unable to communicate with the API! Response: {await res.Content.ReadAsStringAsync()}");
		}

		public async Task<List<ApiMessage>> GetStatusMessages()
		{
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUri("telemetry/launch", _apiVersion));
			HttpResponseMessage res = await _client.GetClient().SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				return new();

			string rawJson = await res.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<ApiMessage>>(rawJson) ?? new();
		}
	}
}
