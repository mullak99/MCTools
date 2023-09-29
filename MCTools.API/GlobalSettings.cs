namespace MCTools.API
{
	public class GlobalSettings
	{
		public TimeOnly ScheduleTime { get; set; }

		public GlobalSettings(IConfiguration configuration)
		{
			TimeOnly.TryParse(configuration["ScheduleTime"], out TimeOnly parsedScheduleTime);
			ScheduleTime = parsedScheduleTime;
		}
	}
}
