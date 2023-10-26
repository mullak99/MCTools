namespace MCTools.SDK.Models
{
	public class MCVersion
	{
		public string Id { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;
		public DateTime Time { get; set; }
		public DateTime ReleaseTime { get; set; }
	}
}
