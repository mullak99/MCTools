using MCTools.SDK.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Threading.Tasks;

namespace MCTools.Shared.Dialog
{
	public partial class ApiStatusModal : LayoutComponentBase
	{
		[CascadingParameter] IMudDialogInstance MudDialog { get; set; }

		private MCToolsHealthStatus Status => MainLayout.ApiStatus;

		private ApiStatus API => MainLayout.ApiStatus.Api;
		private DatabaseStatus Database => MainLayout.ApiStatus.Database;

		protected override Task OnInitializedAsync()
		{
			MainLayout.OnApiStatusChanged += ApiStatusChanged;
			return base.OnInitializedAsync();
		}

		private void ApiStatusChanged(MCToolsHealthStatus obj)
			=> StateHasChanged();

		private void Close()
			=> MudDialog.Close();
	}
}
