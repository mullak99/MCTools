namespace MCTools.API.Extentions
{
	public static class TaskExtensions
	{
		public static async Task WhenAllThrottledAsync(this IEnumerable<Task> tasksToRun, int maxConcurrentTasks, ILogger? logger = null)
		{
			try
			{
				List<Task> allTasks = tasksToRun.ToList();
				List<Task> activeTasks = new(maxConcurrentTasks);

				for (int i = 0; i < maxConcurrentTasks && i < allTasks.Count; i++)
					activeTasks.Add(allTasks[i]);

				for (int i = maxConcurrentTasks; i < allTasks.Count; i++)
				{
					Task completed = await Task.WhenAny(activeTasks.ToArray());
					await completed;

					activeTasks.Remove(completed);
					activeTasks.Add(allTasks[i]);
				}

				await Task.WhenAll(activeTasks.ToArray());
			}
			catch (IOException e)
			{
				logger?.LogError(e, $"{e.Message}");
			}
			catch (Exception e)
			{
				logger?.LogError(e, $"{e.Message}");
				throw;
			}
		}
	}
}
