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

			string fileLogLevelStr = _configuration.GetSection("LogToFileLevel").Value;
			LogLevel fileLogLevel = Enum.Parse<LogLevel>(fileLogLevelStr);

			return new LoggingService(logLevel, fileLogLevel, categoryName);
		}

		public void Dispose()
		{ }
	}
}
