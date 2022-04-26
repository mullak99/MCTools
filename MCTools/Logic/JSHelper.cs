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
        private IJSRuntime JS;

        public JSHelper(IJSRuntime jsRuntime)
        {
            JS = jsRuntime;
        }

        /// <summary>
        /// Copy String List to clipboard
        /// </summary>
        /// <param name="list">List of strings</param>
        public async Task CopyTextToClipboard(List<string> list)
        {
            await JS.InvokeVoidAsync("clipboardCopy.copyText", string.Join(Environment.NewLine, list));
        }

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
    }
}
