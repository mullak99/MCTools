using MCTools.SDK.Enums.Telemetry;
using MCTools.SDK.Extensions;

namespace MCTools.SDK.Models.Telemetry
{
	public class AppInfo
	{
		public string AppId { get; set; } = "UNKNOWN";
		public AppReleaseType ReleaseType { get; set; } = AppReleaseType.Unknown;
		public string Version { get; set; } = "UNKNOWN";
		public string Build { get; set; } = "UNKNOWN";
		public DateTime RequestDateTime { get; set; } = DateTime.UtcNow;

		public void UpdateTime()
			=> RequestDateTime = DateTime.UtcNow;

		public override string ToString()
			=> $"ReleaseType: {ReleaseType.GetDescription()}, Version: {Version}, Build: {Build}";
	}
}
