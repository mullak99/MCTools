using System.ComponentModel;

namespace MCTools.SDK.Enums.Telemetry
{
	public enum ApiMessageSeverity
	{
		[Description("normal")]
		Normal,
		[Description("info")]
		Info,
		[Description("success")]
		Success,
		[Description("warning")]
		Warning,
		[Description("error")]
		Error
	}
}
