namespace MCTools.API.Logging
{
	public class LoggerProvider : ILoggerProvider
	{
		private readonly IConfiguration _configuration;

		public LoggerProvider(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public ILogger CreateLogger(string categoryName)
		{
			string logLevelStr = _configuration.GetSection("Logging:LogLevel:Default").Value;
			LogLevel logLevel = Enum.Parse<LogLevel>(logLevelStr);
			return new LoggingService(logLevel, categoryName);
		}

		public void Dispose()
		{ }
	}
}
