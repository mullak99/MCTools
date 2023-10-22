using MCTools.SDK.Enums.Telemetry;

namespace MCTools.SDK.Models.Telemetry
{
	public class ApiMessage : IApiMessage
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public ApiMessageSeverity Severity { get; set; } = ApiMessageSeverity.Info;
		public string Message { get; set; } = string.Empty;
	}

	public interface IApiMessage
	{
		ApiMessageSeverity Severity { get; set; }
		string Message { get; set; }
	}
}
