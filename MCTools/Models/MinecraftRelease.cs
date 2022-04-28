using System;

namespace MCTools.Models
{
	// TODO: Use M99SDK (M99API)
	public class MinecraftRelease
	{
		public string Version { get; set; }
		public string Type { get; set; }
		public DateTime ReleaseTime { get; set; }
	}
}
