namespace MCTools.SDK.Models
{
	public class MCVersion
	{
		public string Id { get; set; }
		public string Type { get; set; }
		public string Url { get; set; }
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
