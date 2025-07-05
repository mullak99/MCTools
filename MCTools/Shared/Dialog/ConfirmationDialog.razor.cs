using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;

namespace MCTools.Shared.Dialog
{
	public partial class ConfirmationDialog : LayoutComponentBase
	{
		[CascadingParameter] IMudDialogInstance MudDialog { get; set; }

		[Parameter] public string ConfirmationText { get; set; }
		[Parameter] public Action<bool> Callback { get; set; }

		private void Yes()
		{
			Callback(true);
			MudDialog.Close(DialogResult.Ok(true));
		}

		private void No()
		{
			Callback(false);
			MudDialog.Cancel();
		}
	}
}
