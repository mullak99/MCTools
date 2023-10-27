using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Collections.Generic;

namespace MCTools.Shared.Dialog
{
	public partial class ImagePreviewDialog : LayoutComponentBase
	{
		[CascadingParameter] MudDialogInstance MudDialog { get; set; }

		[Parameter] public Dictionary<string, byte[]> Images { get; set; }

		private void Close()
			=> MudDialog.Cancel();
	}
}
