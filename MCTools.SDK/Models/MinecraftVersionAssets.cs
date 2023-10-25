namespace MCTools.SDK.Models
{
	public class MinecraftVersionAssets
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;
		public int Version { get; set; }
		public string Edition { get; set; } = string.Empty;
		public string JSON { get; set; } = string.Empty;
	}
}
