namespace MCTools.SDK.Models
{
	public class MCVersion
	{
		public string Id { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;
		public DateTime Time { get; set; }
		public DateTime ReleaseTime { get; set; }

		public string GetSuffix(MCVersion latestVersion)
		{
			switch (Type)
			{
				case "snapshot":
					return " (Snapshot)";
				case "beta":
					return " (Beta)";
				default:
				{
					if (this == latestVersion)
						return " (Latest)";
					break;
				}
			}
			return string.Empty;
		}
	}
}
