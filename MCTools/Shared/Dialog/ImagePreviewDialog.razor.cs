using MCTools.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MCTools.Shared.Dialog
{
	public partial class ImagePreviewDialog : LayoutComponentBase
	{
		private const float SINGLE_TEX_VIRTUALIZE_SIZE = 245.34f;
		private const float MULTI_TEX_VIRTUALIZE_SIZE = 329.35f;
		private const int TEX_VIRTUALIZE_OVERSCAN = 2;

		[CascadingParameter] MudDialogInstance MudDialog { get; set; }

		[Parameter] public List<DiffImage> Images { get; set; }
		[Parameter] public ConcurrentDictionary<string, byte[]> FromAssets { get; set; }
		[Parameter] public ConcurrentDictionary<string, byte[]> ToAssets { get; set; }

		private List<DiffImage> AddedImages => Images.Where(x => x.Type == DiffImageType.Added).ToList();
		private List<DiffImage> RemovedImages => Images.Where(x => x.Type == DiffImageType.Removed).ToList();
		private List<DiffImage> DifferentImages => Images.Where(x => x.Type == DiffImageType.Different).ToList();

		private void Close()
			=> MudDialog.Cancel();
	}
}
