using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCTools.SDK.Enums.Controllers;
using MCTools.SDK.Models;
using MCTools.Shared.Dialog;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MCTools.Shared
{
	public partial class MainLayout : LayoutComponentBase
	{
		private MudTheme _theme { get; set; }
		private bool _drawerOpen = true;
		private bool _isDarkMode = true;

		public static MCToolsHealthStatus ApiStatus { get; set; } = MCToolsHealthStatus.Unknown;

		public static bool ExpandedVersionSelector { get; set; }

		#if DEBUG
		public const bool DebugMode = true;
		#else
		public static bool DebugMode = false;
		#endif

		public static event Action<MCToolsHealthStatus> OnApiStatusChanged;

		public static bool IsApiHealthy => ApiStatus.Status == Status.Healthy;
		public static bool IsDbHealthy => ApiStatus.Database.Status == Status.Healthy;

		public void ApiStatusChanged(MCToolsHealthStatus status)
		{
			ApiStatus = status;
			OnApiStatusChanged?.Invoke(status);
			StateHasChanged();
		}

		protected override async Task OnInitializedAsync()
		{

			_theme = new()
			{
				PaletteLight = _lightPalette,
				PaletteDark = _darkPalette,
				LayoutProperties = new LayoutProperties()
			};

			List<Task> tasks =
			[
				GetCurrentThemeFromLocalStorage(),
				GetDebugFromLocalStorage(),
				GetExpandedVersionsFromLocalStorage()
			];

			if (Program.IsPreRelease())
			{
				string releaseType = Program.GetReleaseType(true, true);
				tasks.Add(JsHelper.SetTitleAsync($"Minecraft Tools ({releaseType})"));
			}

			await Task.WhenAll(tasks);
			await InvokeAsync(StateHasChanged);

			_ = Task.Run(async () => await TelemetryController.AddAppLaunch(Program.GetAppInfo()));
			_ = Task.Run(UpdateHealthStatus);
		}

		public async Task UpdateHealthStatus()
		{
			var status = await HealthController.GetApiStatus();

			string message = status.Status switch
			{
				Status.Unhealthy => "The API is currently unhealthy! Most features will not work!",
				Status.Degraded => "The API is currently degraded! Some features will not work!",
				_ => null
			};

			if (message is not null)
			{
				var severity = status.Status switch
				{
					Status.Unhealthy => Severity.Error,
					Status.Degraded => Severity.Warning,
					_ => Severity.Normal
				};
				Snackbar.Add(message, severity);
			}
			ApiStatusChanged(status);
		}

		private async Task OpenSettingsDialog()
		{
			DialogOptions options = new() { MaxWidth = MaxWidth.Small, FullWidth = true };
			await Dialog.ShowAsync<SettingsDialog>("Settings", options);
		}

		/// <summary>
		/// Get current theme from the users local storage
		/// </summary>
		/// <returns></returns>
		private async Task GetCurrentThemeFromLocalStorage()
		{
			try
			{
				_isDarkMode = await localStore.GetItemAsync<bool?>("useDarkMode") ?? true;
			}
			catch (Exception)
			{
				Console.WriteLine("An error occurred when loading users theming preference. Using default.");
				_isDarkMode = true;
			}
		}

		/// <summary>
		/// Get debug mode status from the users local storage
		/// </summary>
#pragma warning disable CS1998
		private async Task GetDebugFromLocalStorage()
		{
			#if !DEBUG
			try
			{
				DebugMode = await localStore.GetItemAsync<bool?>("debugMode") ?? false;
			}
			catch (Exception)
			{
				DebugMode = false;
			}
			#endif
		}

		private async Task GetExpandedVersionsFromLocalStorage()
		{
			try
			{
				ExpandedVersionSelector = await localStore.GetItemAsync<bool?>("expandedVersions") ?? false;
			}
			catch (Exception)
			{
				ExpandedVersionSelector = false;
			}
		}

		public static bool IsDebugBuild()
		{
			#if DEBUG
			return true;
			#else
			return false;
			#endif
		}
#pragma warning restore CS1998

		/// <summary>
		/// Toggle between dark and light theme
		/// </summary>
		/// <returns></returns>
		private async Task ToggleTheme()
		{
			_isDarkMode = !_isDarkMode;
			await localStore.SetItemAsync("useDarkMode", _isDarkMode);
		}

		private void ToggleDrawer()
		{
			_drawerOpen = !_drawerOpen;
		}

		private readonly PaletteLight _lightPalette = new()
		{
			Black = "#272c34",
			Background = "#ffffff",
			BackgroundGray = "#f4f4f4",
			Surface = "#efefef",
			DrawerBackground = "#eeeeee",
			AppbarBackground = "#32b432",
			ActionDisabled = "rgba(255,255,255, 0.26)",
			ActionDisabledBackground = "rgba(255,255,255, 0.12)",
			Primary = "#00aa00",
			Dark = "#505050"
		};

		private readonly PaletteDark _darkPalette = new()
		{
			Black = "#27272f",
			Background = "#181818",
			BackgroundGray = "#242424",
			Surface = "#3d3d3d",
			DrawerBackground = "#242424",
			DrawerText = "rgba(255,255,255, 0.75)",
			DrawerIcon = "rgba(255,255,255, 0.75)",
			AppbarBackground = "#303030",
			AppbarText = "rgba(255,255,255, 0.80)",
			TextPrimary = "rgba(255,255,255, 0.80)",
			TextSecondary = "rgba(255,255,255, 0.65)",
			ActionDisabled = "rgba(255,255,255, 0.45)",
			ActionDisabledBackground = "rgba(255,255,255, 0.25)",
			Primary = "#00aa00",
			Dark = "#808080",
			HoverOpacity = 0.2,
			TextDisabled = "rgba(255,255,255, 0.25)"
		};
	}
}
