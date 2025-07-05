using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Threading.Tasks;

namespace MCTools.Shared.Dialog
{
	public partial class SettingsDialog : LayoutComponentBase
	{
		[CascadingParameter] IMudDialogInstance MudDialog { get; set; }

		private bool DebugMode { get; set; } = MainLayout.DebugMode;
		private bool ExpandedVersionSelector { get; set; } = MainLayout.ExpandedVersionSelector;

		private bool _isRefreshNeeded;

		private async Task Apply()
		{
			if (DebugMode != MainLayout.DebugMode)
			{
				await localStore.SetItemAsync<bool?>("debugMode", DebugMode);
				_isRefreshNeeded = true;
			}

			if (ExpandedVersionSelector != MainLayout.ExpandedVersionSelector)
			{
				await localStore.SetItemAsync<bool?>("expandedVersions", ExpandedVersionSelector);
				_isRefreshNeeded = true;
			}
			MudDialog.Close(DialogResult.Ok(true));

			if (_isRefreshNeeded)
				navManager.NavigateTo(navManager.Uri, true);
			else
				StateHasChanged();
		}

		private void Cancel()
			=> MudDialog.Cancel();
	}
}
