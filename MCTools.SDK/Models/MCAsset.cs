namespace MCTools.SDK.Models
{
	public class MCAssets
	{
		public string Name { get; set; }
		public int Version { get; set; }
		public DateTime CreatedDate { get; set; }
		public MinecraftRelease Minecraft { get; set; }
		public List<string> Textures { get; set; }
		public List<string> McMetas { get; set; }
		public bool OverlaySupport { get; set; } = true;
	}
}
