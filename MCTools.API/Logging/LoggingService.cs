namespace MCTools.API.Logging
{
	public class LoggingService : ILogger
	{
		private readonly LogLevel _minLogLevel;
		private readonly LogLevel _fileLogLevel;

		private readonly string _logDirPath = Path.Combine(AppContext.BaseDirectory, "logs");

		private string CategoryName { get; }

		public LoggingService(LogLevel minLogLevel, LogLevel fileLogLevel, string categoryName)
		{
			_minLogLevel = minLogLevel;
			_fileLogLevel = fileLogLevel;
			CategoryName = categoryName;
			Directory.CreateDirectory(_logDirPath);
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return null!;
		}

		public bool IsEnabled(LogLevel logLevel)
			=> logLevel >= _minLogLevel;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(logLevel))
				return;

			SetConsoleColor(logLevel);
			string log = $"{DateTime.Now,-19} [{LogLevelToString(logLevel),8}] [{CategoryName}]: {formatter(state, exception!)}";
			Console.WriteLine(log);
			Console.ResetColor();

			if (logLevel >= _fileLogLevel)
				LogToFile(log);
		}

		public async Task LogAsync<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
		{
			Log(logLevel, eventId, state, exception, formatter);
			await Task.CompletedTask;
		}

		private void LogToFile(string log)
		{
			string path = Path.Combine(_logDirPath, $"{DateTime.Now:yyyy-MM-dd}.log");
			File.WriteAllText(path, $"{log}\n");
		}

		private static string LogLevelToString(LogLevel logLevel)
		{
			return logLevel switch
			{
				LogLevel.Trace => "Trace",
				LogLevel.Debug => "Debug",
				LogLevel.Information => "Info",
				LogLevel.Warning => "Warn",
				LogLevel.Error => "Error",
				LogLevel.Critical => "Critical",
				_ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
			};
		}

		private static void SetConsoleColor(LogLevel severity)
		{
			switch (severity)
			{
				case LogLevel.Critical:
				case LogLevel.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
				case LogLevel.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case LogLevel.Information:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case LogLevel.Trace:
				case LogLevel.Debug:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
			}
		}
	}
}
