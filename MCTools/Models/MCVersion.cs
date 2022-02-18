using System;

namespace MCTools.Models
{
    // TODO: Use M99SDK
    public class MCVersion
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public DateTime Time { get; set; }
        public DateTime ReleaseTime { get; set; }
    }
}
