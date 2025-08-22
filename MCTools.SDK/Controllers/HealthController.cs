using MCTools.SDK.Interfaces.Controllers;
using MCTools.SDK.Models;
using Newtonsoft.Json;

namespace MCTools.SDK.Controllers
{
	public class HealthController : IController
	{
		private IApiClient _client { get; }

		public HealthController(IApiClient client)
		{
			_client = client;
		}

		public async Task<MCToolsHealthStatus> GetApiStatus(uint timeoutMs = 5000)
		{
			using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMilliseconds(timeoutMs));
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUriRaw("health/summary"));

			try
			{
				HttpResponseMessage res = await _client.GetClient().SendAsync(req, cancellationTokenSource.Token);

				string rawJson = await res.Content.ReadAsStringAsync(cancellationTokenSource.Token);
				return JsonConvert.DeserializeObject<MCToolsHealthStatus>(rawJson) ?? MCToolsHealthStatus.Unknown;
			}
			catch (TaskCanceledException)
			{
				// The task was canceled due to timeout
				return MCToolsHealthStatus.Unhealthy;
			}
		}
	}
}
