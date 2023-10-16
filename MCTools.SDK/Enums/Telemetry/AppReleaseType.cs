using System.ComponentModel;

namespace MCTools.SDK.Enums.Telemetry
{
	public enum AppReleaseType
	{
		[Description("None")]
		None = 0,

		[Description("Stable")]
		Stable = 1,

		[Description("Beta")]
		Beta = 2,

		[Description("Dev")]
		Dev = 3,

		[Description("Unknown")]
		Unknown = 99
	}
}
