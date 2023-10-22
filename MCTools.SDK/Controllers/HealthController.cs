using MCTools.SDK.Interfaces.Controllers;
using System.Net;

namespace MCTools.SDK.Controllers
{
	public class HealthController : IController
	{
		private IApiClient _client { get; }

		public HealthController(IApiClient client)
		{
			_client = client;
		}

		public async Task<HttpStatusCode> GetApiStatus(uint timeoutMs = 2000)
		{
			using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMilliseconds(timeoutMs));
			HttpRequestMessage req = new(HttpMethod.Get, _client.BuildRequestUriRaw("health"));

			try
			{
				HttpResponseMessage res = await _client.GetClient().SendAsync(req, cancellationTokenSource.Token);
				return res.StatusCode;
			}
			catch (TaskCanceledException)
			{
				// The task was canceled due to timeout
				return HttpStatusCode.BadGateway;
			}
		}
	}
}
