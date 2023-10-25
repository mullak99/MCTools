namespace MCTools.SDK.Models
{
	public class MinecraftRelease
	{
		public string Version { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public DateTime ReleaseTime { get; set; }
		public string Edition { get; set; } = string.Empty;
	}
}
