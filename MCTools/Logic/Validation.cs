using MCTools.Enums;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Linq;

namespace MCTools.Logic
{
	public class Validation
	{
		private const int MAX_FILESIZE_MB = 100;
		private const int MAX_FILESIZE_BYTES = MAX_FILESIZE_MB * 1024 * 1024;

		/// <summary>
		/// Ensure the uploaded file is valid
		/// </summary>
		/// <param name="edition">Minecraft edition to verify against</param>
		/// <param name="file">Uploaded file</param>
		/// <returns>A list of errors</returns>
		public static List<string> PackValidation(MCEdition edition, IBrowserFile file)
		{
			List<string> errors = new();
			if (file.Size > MAX_FILESIZE_BYTES) // Limit max filesize for resource pack
				errors.Add($"Uploads cannot be greater than {MAX_FILESIZE_MB}MB");

			string fileType = file.Name.Split('.').Last();
			if (edition == MCEdition.Java)
			{
				if (fileType != "zip") // Only support zip files for Java
					errors.Add("Only zip files are supported");
			}
			else
			{
				if (fileType is not ("zip" or "mcpack")) // Only support zip & mcpack files for Bedrock
					errors.Add("Only zip and mcpack files are supported");
			}
			return errors;
		}

		public static string GetSupportedPackFormats(MCEdition edition)
			=> edition == MCEdition.Java ? ".zip" : ".zip, .mcpack";

		public static int GetMaxFileSizeMB() => MAX_FILESIZE_MB;
		public static int GetMaxFileSizeBytes() => MAX_FILESIZE_BYTES;
	}
}
