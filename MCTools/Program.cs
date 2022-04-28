using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using MCTools.Controllers;
using Microsoft.Extensions.Configuration;
using MudBlazor;

namespace MCTools
{
	public class Program
	{
		private static bool IsBetaRelease;
		private const byte BetaTag = 1;

		private static string StableUrl;
		private static string BetaUrl;

		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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

			string releaseType = builder.Configuration.GetValue<string>("Application:ReleaseType");
			IsBetaRelease = releaseType.ToUpper() == "BETA";
			Console.WriteLine($"Release Type: {releaseType}");

			var environment = builder.Configuration.GetValue<string>("Application:Environment");
			var url = builder.Configuration.GetValue<string>($"Endpoint:{environment}");
			Console.WriteLine($"API Endpoint: {url}");

			StableUrl = builder.Configuration.GetValue<string>("Urls:Stable");
			BetaUrl = builder.Configuration.GetValue<string>("Urls:Beta");

			builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(url) });
			builder.Services.AddScoped<ApiController>();

			await builder.Build().RunAsync();
		}

		public static bool IsBeta()
		{
			return IsBetaRelease;
		}

		public static string GetVersion()
		{
			Version version = Assembly.GetEntryAssembly().GetName().Version;
			string verString = $"v{version.Major}.{version.Minor}.{version.Revision}";

			if (IsBeta())
				verString += $"-BETA{BetaTag}";

			return verString;
		}

		public static string GetStableUrl()
		{
			return StableUrl;
		}

		public static string GetBetaUrl()
		{
			return BetaUrl;
		}
	}
}
