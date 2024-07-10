using MCTools.SDK.Models;
using MudBlazor;

namespace MCTools.Extensions
{
	public static class MCVersionExtension
	{
		public static string GetIcon(this MCVersion version, MCVersion latestVersion)
		{
			switch (version.Type)
			{
				case "snapshot":
				case "beta":
					return Icons.Material.Filled.Science;
				default:
				{
					if (version == latestVersion)
						return Icons.Material.Filled.Grade;
					break;
				}
			}
			return string.Empty;
		}

		public static string GetSuffix(this MCVersion version, MCVersion latestVersion)
		{
			string suffix = version.GetDetailedType(latestVersion);
			return !string.IsNullOrWhiteSpace(suffix) ? $" ({suffix})" : string.Empty;
		}

		public static string GetDetailedType(this MCVersion version, MCVersion latestVersion)
		{
			switch (version.Type)
			{
				case "snapshot":
					return "Snapshot";
				case "beta":
					return "Beta";
				default:
				{
					if (version == latestVersion)
						return "Latest";
					break;
				}
			}
			return string.Empty;
		}
	}
}
