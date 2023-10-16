using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MCTools.SDK.Models.Telemetry;

namespace MCTools.Controllers
{
	public class TelemetryController
	{
		private HttpClient _client { get; }

		public TelemetryController(HttpClient client)
		{
			_client = client;
		}

		public async Task AddAppLaunch()
		{
			HttpRequestMessage req = new(HttpMethod.Post, $"{_client.BaseAddress}api/v1.0/telemetry/launch");
			req.Content = new StringContent(JsonConvert.SerializeObject(Program.GetAppInfo()), Encoding.UTF8, "application/json");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode != System.Net.HttpStatusCode.OK)
				Console.WriteLine($"Unable to communicate with the API! Response: {await res.Content.ReadAsStringAsync()}");
		}

		public async Task<List<ApiMessage>> GetStatusMessages()
		{
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}api/v1.0/telemetry/statusmessage/getall");
			HttpResponseMessage res = await _client.SendAsync(req);

			if (res.StatusCode == System.Net.HttpStatusCode.OK)
			{
				string rawJson = await res.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<List<ApiMessage>>(rawJson);
			}
			return new();
		}
	}
}
