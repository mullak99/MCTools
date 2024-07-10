namespace MCTools.SDK.Models.Telemetry
{
	public class AppError
	{
		public Guid Id { get; set; } = Guid.Empty;
		public AppInfo? AppInfo { get; set; } = null;
		public string ExceptionType { get; set; } = "UNKNOWN";
		public string ExceptionMessage { get; set; } = "UNKNOWN";
		public string ExceptionStackTrace { get; set; } = "UNKNOWN";
	}
}
