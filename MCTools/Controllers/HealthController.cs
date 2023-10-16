using MCTools.Enums;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MCTools.Controllers
{
	public class HealthController
	{
		private HttpClient _client { get; }

		public HealthController(HttpClient client)
		{
			_client = client;
		}

		public async Task<ApiStatus> GetApiStatus(uint timeoutMs = 2000)
		{
			using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
			HttpRequestMessage req = new(HttpMethod.Get, $"{_client.BaseAddress}health");

			try
			{
				HttpResponseMessage res = await _client.SendAsync(req, cancellationTokenSource.Token);
				return res.StatusCode == System.Net.HttpStatusCode.OK ? ApiStatus.Online : ApiStatus.Offline;
			}
			catch (TaskCanceledException)
			{
				// The task was canceled due to timeout
				return ApiStatus.Offline;
			}
		}


	}
}
