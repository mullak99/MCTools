namespace MCTools.SDK.Models.Telemetry
{
	public class AppAction
	{
		public Guid RequestId { get; set; } = Guid.NewGuid();
		public string Action { get; set; } = "UNKNOWN";
		public List<string> Details { get; set; } = new();
		public DateTime RequestDateTime { get; set; } = DateTime.UtcNow;
	}
}
