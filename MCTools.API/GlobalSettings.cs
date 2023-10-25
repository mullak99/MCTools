namespace MCTools.API
{
	public class GlobalSettings
	{
		public TimeOnly ScheduleTime { get; set; }
		public bool TelemetryIgnoreDev { get; set; } = true;
		public string DbNameSuffix { get; set; } = string.Empty;

		public GlobalSettings(IConfiguration configuration)
		{
			if (TimeOnly.TryParse(configuration["Settings:ScheduleTime"], out TimeOnly parsedScheduleTime))
				ScheduleTime = parsedScheduleTime;

			if (bool.TryParse(configuration["Settings:TelemetryIgnoreDev"], out bool parsedTeleIgnoreDev))
				TelemetryIgnoreDev = parsedTeleIgnoreDev;

			if (!string.IsNullOrWhiteSpace(configuration["Settings:DbNameSuffix"]))
				DbNameSuffix = configuration["Settings:DbNameSuffix"];
		}
	}
}
