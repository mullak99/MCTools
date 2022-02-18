using System;
using System.Collections.Generic;

namespace MCTools.Models
{
    // TODO: Use M99SDK
    public class MCAssets
    {
        public string Name { get; set; }
        public int Version { get; set; }
        public DateTime CreatedDate { get; set; }
        public MinecraftRelease Minecraft { get; set; }
        public List<string> Textures { get; set; }
    }
}
