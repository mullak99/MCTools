using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MCTools.Shared
{
	public partial class MainLayout : LayoutComponentBase
	{
		public MudTheme CurrentTheme { get; set; }
		public bool IsDarkMode { get; set; } = true;
		public bool IsDrawerOpen { get; set; } = true;

		#if DEBUG
		public static bool DebugMode = true;
		#else
		public static bool DebugMode = false;
		#endif

		private MudTheme GetCurrentTheme() => IsDarkMode ? DarkTheme : DefaultTheme;

		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(GetCurrentThemeFromLocalStorage(), GetDebugFromLocalStorage());
			await InvokeAsync(StateHasChanged);
		}

		/// <summary>
		/// Get current theme from the users local storage
		/// </summary>
		/// <returns></returns>
		private async Task GetCurrentThemeFromLocalStorage()
		{
			try
			{
				IsDarkMode = await localStore.GetItemAsync<bool?>("useDarkMode") ?? true;
			}
			catch (Exception)
			{
				Console.WriteLine("An error occurred when loading users theming preference. Using default.");
				IsDarkMode = true;
			}
			CurrentTheme = GetCurrentTheme();
		}

		/// <summary>
		/// Get debug mode status from the users local storage
		/// </summary>
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

		/// <summary>
		/// Toggle between dark and light theme
		/// </summary>
		/// <returns></returns>
		private async Task ToggleTheme()
		{
			IsDarkMode = !IsDarkMode;
			CurrentTheme = GetCurrentTheme();
			await localStore.SetItemAsync("useDarkMode", IsDarkMode);
		}

		private void ToggleDrawer()
		{
			IsDrawerOpen = !IsDrawerOpen;
		}

		private MudTheme DefaultTheme => new MudTheme()
		{
			Palette = new Palette()
			{
				Black = "#272c34",
				Background = "#ffffff",
				BackgroundGrey = "#f4f4f4",
				Surface = "#efefef",
				DrawerBackground = "#eeeeee",
				AppbarBackground = "#32b432",
				ActionDefault = "#2dd22d",
				ActionDisabled = "rgba(255,255,255, 0.26)",
				ActionDisabledBackground = "rgba(255,255,255, 0.12)",
				Primary = "#00aa00",
				Dark = "#505050"
			}
		};

		private MudTheme DarkTheme => new MudTheme()
		{
			Palette = new Palette()
			{
				Black = "#27272f",
				Background = "#181818",
				BackgroundGrey = "#242424",
				Surface = "#3d3d3d",
				DrawerBackground = "#242424",
				DrawerText = "rgba(255,255,255, 0.75)",
				DrawerIcon = "rgba(255,255,255, 0.75)",
				AppbarBackground = "#303030",
				AppbarText = "rgba(255,255,255, 0.80)",
				TextPrimary = "rgba(255,255,255, 0.80)",
				TextSecondary = "rgba(255,255,255, 0.65)",
				ActionDefault = "#2dd22d",
				ActionDisabled = "rgba(255,255,255, 0.45)",
				ActionDisabledBackground = "rgba(255,255,255, 0.25)",
				Primary = "#00aa00",
				Dark = "#808080",
				HoverOpacity = 0.2,
				TextDisabled = "rgba(255,255,255, 0.25)"
			}
		};
	}
}
