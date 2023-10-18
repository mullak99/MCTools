using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCTools.Logic
{
	public class InputOutputUtils
	{
		/// <summary>
		/// Copy String List to clipboard
		/// </summary>
		/// <param name="jsHelper">JSHelper</param>
		/// <param name="list">List of strings</param>
		public static async Task CopyTextToClipboard(JSHelper jsHelper, List<string> list)
			=> await jsHelper.CopyTextToClipboard(list);

		/// <summary>
		/// Export a text file containing a list of strings
		/// </summary>
		/// <param name="jsHelper">JSHelper</param>
		/// <param name="listToExport">List of string to export (each on a new line)</param>
		/// <param name="fileName">Exported files filename</param>
		public static async Task Export(JSHelper jsHelper, List<string> listToExport, string fileName)
			=> await jsHelper.ExportListToFile(listToExport, fileName);
	}
}
