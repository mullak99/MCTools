using Auth0.ManagementApi;
using MCTools.API.Abstractions;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace MCTools.API.Services
{
	public interface ICurrentUserService
	{
		bool CurrentUserHasRole(ClaimsIdentity user, string name);
		bool CurrentUserHasPermission(ClaimsIdentity user, string permission);
		List<string> GetUserPermissions(ClaimsIdentity user);
		List<string> GetPermissions();
	}

	public class CurrentUserService : ICurrentUserService, IRoleValidator, IPermissionValidator
	{
		private readonly ManagementApiClient _managementApiClient;
		private readonly string _auth0ManagementApiAudience;

		public CurrentUserService(ManagementApiClient managementApiClient, string auth0ManagementApiAudience)
		{
			_managementApiClient = managementApiClient;
			_auth0ManagementApiAudience = auth0ManagementApiAudience;
		}

		public static async Task<CurrentUserService> CreateAsync(IConfiguration configuration, ILogger<CurrentUserService> logger)
		{
			var auth0ManagementApiAudience = configuration["Auth0:Audience"] ?? throw new NullReferenceException("Auth0 Audience not configured!");
			var auth0ManagementApiClientId = configuration["Auth0:ClientId"] ?? throw new NullReferenceException("Auth0 ClientId not configured!");
			var auth0ManagementApiClientSecret = configuration["Auth0:ClientSecret"] ?? throw new NullReferenceException("Auth0 ClientSecret not configured!");
			var auth0Domain = configuration["Auth0:Authority"] ?? throw new NullReferenceException("Auth0 Authority not configured!");

			var managementApiToken = await GetManagementApiTokenAsync(auth0Domain, auth0ManagementApiClientId, auth0ManagementApiClientSecret, logger);
			var managementApiBaseUri = new Uri($"{auth0Domain}/api/v2/");
			var managementApiClient = new ManagementApiClient(managementApiToken, managementApiBaseUri);

			return new CurrentUserService(managementApiClient, auth0ManagementApiAudience);
		}

		private static async Task<string> GetManagementApiTokenAsync(string auth0Domain, string managementApiClientId, string managementApiClientSecret, ILogger<CurrentUserService> logger)
		{
			using var httpClient = new HttpClient();
			var tokenRequest = new Dictionary<string, string>
			{
				["grant_type"] = "client_credentials",
				["client_id"] = managementApiClientId,
				["client_secret"] = managementApiClientSecret,
				["audience"] = $"{auth0Domain}/api/v2/"
			};
			var tokenResponse = await httpClient.PostAsync($"{auth0Domain}/oauth/token", new FormUrlEncodedContent(tokenRequest));

			if (!tokenResponse.IsSuccessStatusCode)
			{
				var errorResponseContent = await tokenResponse.Content.ReadAsStringAsync();
				var errorResponseJson = JObject.Parse(errorResponseContent);
				var errorMessage = errorResponseJson["error_description"]?.ToString() ?? string.Empty;
				logger.LogWarning($"Error obtaining Management API access token: {errorMessage}");

				return string.Empty;
			}

			var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
			var tokenResponseJson = JObject.Parse(tokenResponseContent);
			return tokenResponseJson["access_token"]?.ToString() ?? string.Empty;
		}

		public bool CurrentUserHasRole(ClaimsIdentity user, string name)
		{
			return user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == name);
		}

		public bool CurrentUserHasPermission(ClaimsIdentity user, string permission)
		{
			return user.HasClaim(c => c.Type == "permissions" && c.Value == permission);
		}

		public List<string> GetUserPermissions(ClaimsIdentity user)
		{
			var permissions = user.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();
			return permissions;
		}

		public List<string> GetPermissions()
		{
			return new List<string>()
			{
				"admin-action",
				"write:pregenerate-assets",
				"write:purge-assets",
				"read:telemetry",
				"delete:telemetry",
				"write:effectitem",
				"delete:effectitem",
				"write:statusmessage",
				"delete:statusmessage"
			};
		}
	}
}
