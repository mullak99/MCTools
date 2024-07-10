using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MCTools.Logic
{
	public class JSHelper
	{
		private readonly IJSRuntime JS;

		public JSHelper(IJSRuntime jsRuntime)
		{
			JS = jsRuntime;
		}

		/// <summary>
		/// Copy String List to clipboard
		/// </summary>
		/// <param name="list">List of strings</param>
		public async Task CopyTextToClipboard(List<string> list)
			=> await JS.InvokeVoidAsync("clipboardCopy.copyText", string.Join(Environment.NewLine, list));

		/// <summary>
		/// Export a text file containing a list of strings
		/// </summary>
		/// <param name="listToExport">List of string to export (each on a new line)</param>
		/// <param name="fileName">Exported files filename</param>
		public async Task ExportListToFile(List<string> listToExport, string fileName)
		{
			using var streamRef = new DotNetStreamReference(new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", listToExport))));
			await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
		}

		/// <summary>
		/// Download a file from a byte array
		/// </summary>
		/// <param name="fileName">Output filename</param>
		/// <param name="contents">Contents of the file</param>
		public async Task DownloadZip(string fileName, byte[] contents)
			=> await JS.InvokeVoidAsync("BlazorDownloadFile", fileName, "application/zip", Convert.ToBase64String(contents));

		/// <summary>
		/// Open a link in a new tab
		/// </summary>
		/// <param name="url">Link to open</param>
		public async Task OpenLinkInNewTab(string url)
			=> await JS.InvokeVoidAsync("openInNewTab", url);

		/// <summary>
		/// Set title of the page
		/// </summary>
		/// <param name="title">New title</param>
		public async Task SetTitleAsync(string title)
			=> await JS.InvokeVoidAsync("setTitle", title);
	}
}
