using MCTools.SDK.Enums.Telemetry;

namespace MCTools.SDK.Models.Telemetry
{
	public class ApiMessage
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public ApiMessageSeverity Severity { get; set; } = ApiMessageSeverity.Info;
		public string Message { get; set; } = string.Empty;
	}
}
