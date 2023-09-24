using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Threading.Tasks;

namespace MCTools.Shared.Dialog
{
	public partial class SettingsDialog : LayoutComponentBase
	{
		[CascadingParameter] MudDialogInstance MudDialog { get; set; }

		private bool DebugMode { get; set; } = MainLayout.DebugMode;

		private bool _isRefreshNeeded;

		private async Task Apply()
		{
			if (DebugMode != MainLayout.DebugMode)
			{
				await localStore.SetItemAsync<bool?>("debugMode", DebugMode);
				_isRefreshNeeded = true;
			}

			MudDialog.Close(DialogResult.Ok(true));

			if (_isRefreshNeeded)
				navManager.NavigateTo(navManager.Uri, true);
		}

		private void Cancel()
			=> MudDialog.Cancel();
	}
}
