namespace MCTools.SDK.Models
{
	public class MCAssets
	{
		public string Name { get; set; } = string.Empty;
		public int Version { get; set; }
		public DateTime CreatedDate { get; set; }
		public MinecraftRelease Minecraft { get; set; } = null!;
		public List<string> Textures { get; set; } = new();
		public List<string> McMetas { get; set; } = new();
		public bool OverlaySupport { get; set; } = true;
	}
}
