using Blazored.LocalStorage;
using MCTools.Controllers;
using MCTools.Logic;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using System;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using MCTools.SDK.Enums.Telemetry;
using MCTools.SDK.Models.Telemetry;

namespace MCTools
{
	public class Program
	{
		private static bool _isPreRelease;
		private static byte _preReleaseTag = 1; // Placeholder. Appsettings will override this.
		private static string _releaseType;
		private static string _gitTag = "dev";

		private static string StableUrl;
		private static string BetaUrl;

		public static string BaseAddress;
		public static string ApiAddress;

		private static AppInfo _appInfo;

		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");

			builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

			builder.Services.AddMudServices(config =>
				{
					config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
					config.SnackbarConfiguration.PreventDuplicates = false;
					config.SnackbarConfiguration.NewestOnTop = false;
					config.SnackbarConfiguration.ShowCloseIcon = true;
					config.SnackbarConfiguration.VisibleStateDuration = 5000;
					config.SnackbarConfiguration.HideTransitionDuration = 250;
					config.SnackbarConfiguration.ShowTransitionDuration = 250;
					config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
				});

			builder.Services.AddBlazoredLocalStorage();

			_releaseType = builder.Configuration["Application:ReleaseType"] ?? "Stable";
			_isPreRelease = _releaseType.ToUpper() != "STABLE";
			_gitTag = builder.Configuration["Application:GitTag"] ?? "dev";
			Console.WriteLine($"Release Type: {_releaseType}");

			_ = byte.TryParse(builder.Configuration["Application:PreReleaseTag"], out _preReleaseTag);

			string environment = builder.Configuration["Application:Environment"] ?? "Production";

			string apiType = environment switch
			{
				"Production" => _isPreRelease ? "Beta" : "Stable",
				"Development" => "Development",
				_ => "Stable"
			};

			ApiAddress = builder.Configuration[$"Endpoint:{apiType}"];
			Console.WriteLine($"API Endpoint: {ApiAddress}");

			StableUrl = builder.Configuration["Urls:Stable"];
			BetaUrl = builder.Configuration["Urls:Beta"];

			builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(ApiAddress ?? string.Empty) });
			builder.Services.AddScoped<ApiController>();
			builder.Services.AddScoped<TelemetryController>();
			builder.Services.AddScoped<JSHelper>();

			BaseAddress = builder.HostEnvironment.BaseAddress;

			_appInfo = new AppInfo
			{
				AppId = "MCTools",
				ReleaseType = apiType switch
				{
					"Stable" => AppReleaseType.Stable,
					"Beta" => AppReleaseType.Beta,
					"Development" => AppReleaseType.Dev,
					_ => AppReleaseType.Unknown
				},
				Version = GetVersion(),
				Build = _gitTag
			};

			await builder.Build().RunAsync();
		}

		public static AppInfo GetAppInfo()
			=> _appInfo;

		public static bool IsPreRelease()
			=> _isPreRelease;

		public static string GetReleaseType(bool hideStable = false, bool toTitleCase = false)
		{
			string relType = toTitleCase ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_releaseType) : _releaseType.ToUpper();
			return IsPreRelease() ? relType : hideStable ? string.Empty : relType;
		}

		public static string GetVersion(bool includeGitTag = false)
		{
			Version version = Assembly.GetEntryAssembly()?.GetName().Version;
			if (version == null)
				return "UNKNOWN";

			string verString = $"v{version.Major}.{version.Minor}.{version.Build}";
			if (IsPreRelease())
				verString += $"-{GetReleaseType(true)}{_preReleaseTag}";
			if (includeGitTag)
				verString += $" ({GetGitTag()})";

			return verString;
		}

		public static string GetGitTag()
			=> _gitTag;

		public static string GetStableUrl()
			=> StableUrl;

		public static string GetBetaUrl()
			=> BetaUrl;

		public static string GetUrl()
			=> IsPreRelease() ? GetBetaUrl() : GetStableUrl();
	}
}
