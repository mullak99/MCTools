using MCTools.API.Extentions;
using MCTools.API.Logic;

namespace MCTools.API.Services
{
	public class ScheduledService : BackgroundService
	{
		private readonly DateTime _scheduledTime;
		private readonly List<Func<Task>> _scheduledTasks = new();
		private readonly object _lock = new();

		private readonly IServiceScopeFactory _scopeFactory;
		private readonly GlobalSettings _globalSettings;
		private readonly ILogger<ScheduledService> _logger;

		public ScheduledService(IServiceScopeFactory scopeFactory, GlobalSettings globalSettings, ILogger<ScheduledService> logger)
		{
			_scopeFactory = scopeFactory;
			_globalSettings = globalSettings;
			_scheduledTime = GetNextDateTime(_globalSettings.ScheduleTime);
			_logger = logger;
		}

		private static DateTime GetNextDateTime(TimeOnly time)
		{
			DateTime today = DateTime.UtcNow.Date;
			TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);

			// If the specified time is later than or equal to the current time, return a DateTime with today's date and the specified time.
			if (time >= currentTime)
				return today + time.ToTimeSpan();

			// Otherwise, return a DateTime with tomorrow's date and the specified time.
			return today.AddDays(1) + time.ToTimeSpan();
		}

		public void AddScheduledTask(Func<Task> task)
		{
			lock (_lock)
			{
				_scheduledTasks.Add(task);
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
			=> await ScheduleTask(stoppingToken);

		private async Task ScheduleTask(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				DateTime currentTime = DateTime.Now;
				TimeSpan timeUntilScheduledTime = _scheduledTime - currentTime;

				if (timeUntilScheduledTime < TimeSpan.Zero)
					timeUntilScheduledTime = TimeSpan.Zero;

				_logger.LogInformation($"Schedule will run at {_scheduledTime:hh\\:mm tt} (UTC). This is in {timeUntilScheduledTime.Hours}h {timeUntilScheduledTime.Minutes}m {timeUntilScheduledTime.Seconds}s.");
				await Task.Delay(timeUntilScheduledTime, stoppingToken);

				// Call your desired method here
				await ExecuteScheduledTasks();

				// Schedule the task for the next day at the same time
				DateTime nextExecutionTime = DateTime.Today.AddDays(1).Add(_scheduledTime.TimeOfDay);
				TimeSpan delayUntilNextExecution = nextExecutionTime - DateTime.Now;
				await Task.Delay(delayUntilNextExecution, stoppingToken);
			}
		}

		private async Task ExecuteScheduledTasks()
		{
			_logger.LogInformation("Executing scheduled tasks...");

			Func<Task>[] tasksToExecute;
			lock (_lock)
			{
				tasksToExecute = _scheduledTasks.ToArray();
				_scheduledTasks.Clear();
			}
			await tasksToExecute.Select(x => x()).WhenAllThrottledAsync(5);
			await PregenerateAndPurgeTask();

			_logger.LogInformation("Finished executing scheduled tasks.");
		}

		private async Task PregenerateAndPurgeTask()
		{
			using IServiceScope scope = _scopeFactory.CreateScope();
			IToolsLogic toolsLogic = scope.ServiceProvider.GetRequiredService<IToolsLogic>();

			_logger.LogDebug("Purging old assets...");
			long versionsPurged = await toolsLogic.PurgeAssets();
			if (versionsPurged > 0)
				_logger.LogInformation($"Purged {versionsPurged} old versions!");

			List<Task> tasks = new()
			{
				toolsLogic.PregenerateJavaAssets(bypassHighestVersionLimit: true),
				toolsLogic.PregenerateBedrockAssets()
			};

			_logger.LogDebug("Pregenerating new assets...");
			await Task.WhenAll(tasks);
		}
	}
}
