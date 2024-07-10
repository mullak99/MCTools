namespace MCTools.SDK.Models
{
	public class AssetMCVersion
	{
		public string Id { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public string Edition { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;
		public DateTime Time { get; set; }
		public DateTime ReleaseTime { get; set; }
	}
}
