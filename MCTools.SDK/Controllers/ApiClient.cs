using MCTools.SDK.Enums.Controllers;

namespace MCTools.SDK.Controllers
{
	public class ApiClient : IApiClient
	{
		private readonly HttpClient _client;
		private readonly string _baseAddress;

		public ApiClient(HttpClient client, ApiRelease apiRelease = ApiRelease.Release, string overrideBaseAddress = "")
		{
			_client = client;
			_baseAddress = apiRelease switch
			{
				ApiRelease.Release => "https://mctools-api.mullak99.co.uk",
				ApiRelease.Beta => "https://mctools-api-beta.mullak99.co.uk",
				_ => overrideBaseAddress.TrimEnd('/')
			};
			_client.BaseAddress = new Uri(_baseAddress);
		}

		public string BuildRequestUri(string requestUri, string apiVersion = "1.0")
			=> BuildRequestUriRaw($"api/v{apiVersion}/{requestUri.TrimStart('/')}");

		public string BuildRequestUriRaw(string requestUri)
			=> $"{_baseAddress.TrimEnd('/')}/{requestUri.TrimStart('/')}";

		public HttpClient GetClient()
			=> _client;

		public string GetBaseAddress()
			=> _baseAddress;
	}

	public interface IApiClient
	{
		string BuildRequestUri(string requestUri, string apiVersion);
		string BuildRequestUriRaw(string requestUri);
		HttpClient GetClient();
		string GetBaseAddress();
	}
}
