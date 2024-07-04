using MCTools.API.Repository;
using MCTools.SDK.Enums.Telemetry;
using MCTools.SDK.Models.Telemetry;

namespace MCTools.API.Logic
{
	public class TelemetryLogic : ITelemetryLogic
	{
		private readonly ITelemetryRepository _telemetryRepository;
		private readonly GlobalSettings _globalSettings;
		private readonly ILogger<TelemetryLogic> _logger;

		public TelemetryLogic(ITelemetryRepository telemetryRepository, GlobalSettings globalSettings, ILogger<TelemetryLogic> logger)
		{
			_telemetryRepository = telemetryRepository;
			_globalSettings = globalSettings;
			_logger = logger;
		}

		#region AppInfo
		public async Task AddAppVisit(AppInfo appInfo)
		{
			switch (appInfo.ReleaseType)
			{
				case AppReleaseType.Dev when _globalSettings.TelemetryIgnoreDev:
					return;
				case AppReleaseType.None:
				case AppReleaseType.Unknown:
					_logger.LogWarning($"App visit with unknown release type! AppInfo: {appInfo}");
					return;
			}

			appInfo.UpdateTime();
			_logger.LogInformation($"New app visit! {appInfo}");
			await _telemetryRepository.AddAppVisit(appInfo);
		}

		public async Task<List<AppInfo>> GetAppVisits()
			=> await _telemetryRepository.GetAllAppVisits();

		public async Task<List<AppInfo>> GetAppVisits(AppReleaseType releaseType)
			=> await _telemetryRepository.GetAllAppVisits(releaseType);

		public async Task<List<AppInfo>> GetAppVisits(DateTime from, DateTime to)
			=> await _telemetryRepository.GetAllAppVisits(from, to);

		public async Task<List<AppInfo>> GetAppVisits(AppReleaseType releaseType, DateTime from, DateTime to)
			=> await _telemetryRepository.GetAllAppVisits(releaseType, from, to);

		public async Task<long> GetAppVisitsCount()
			=> await _telemetryRepository.GetAppVisitsCount();

		public async Task<long> GetAppVisitsCount(AppReleaseType releaseType)
			=> await _telemetryRepository.GetAppVisitsCount(releaseType);

		public async Task<long> GetAppVisitsCount(DateTime from, DateTime to)
			=> await _telemetryRepository.GetAppVisitsCount(from, to);

		public async Task<long> GetAppVisitsCount(AppReleaseType releaseType, DateTime from, DateTime to)
			=> await _telemetryRepository.GetAppVisitsCount(releaseType, from, to);

		public async Task<long> PurgeAppVisits()
		{
			_logger.LogInformation("Purging app visits...");
			return await _telemetryRepository.DeleteAllAppVisits();
		}

		public async Task<bool> AddAppAction(Guid sessionId, AppAction appAction)
			=> await _telemetryRepository.AddAppAction(sessionId, appAction);

		public async Task<List<AppAction>?> GetAppActions(Guid sessionId)
			=> (await GetAppVisits()).Find(x => x.SessionId == sessionId)?.Actions;
		#endregion

		#region AppError
		public async Task AddAppError(AppError appError)
		{
			switch (appError.AppInfo?.ReleaseType)
			{
				case AppReleaseType.Dev when _globalSettings.TelemetryIgnoreDev:
					return;
				case AppReleaseType.None:
				case AppReleaseType.Unknown:
					_logger.LogDebug($"App error with unknown release type! AppInfo: {appError.AppInfo}");
					return;
			}

			appError.AppInfo?.UpdateTime();
			await _telemetryRepository.AddAppError(appError);
		}

		public async Task<List<AppError>> GetAppErrors()
			=> await _telemetryRepository.GetAllAppErrors();

		public async Task<long> GetAppErrorCount()
			=> await _telemetryRepository.GetAppErrorCount();

		public async Task<long> PurgeAppErrors()
		{
			_logger.LogInformation("Purging app errors...");
			return await _telemetryRepository.DeleteAllAppErrors();
		}
		#endregion
	}

	public interface ITelemetryLogic
	{
		#region AppInfo
		Task AddAppVisit(AppInfo appInfo);

		Task<List<AppInfo>> GetAppVisits();
		Task<List<AppInfo>> GetAppVisits(AppReleaseType releaseType);
		Task<List<AppInfo>> GetAppVisits(DateTime from, DateTime to);
		Task<List<AppInfo>> GetAppVisits(AppReleaseType releaseType, DateTime from, DateTime to);
		Task<long> GetAppVisitsCount();
		Task<long> GetAppVisitsCount(AppReleaseType releaseType);
		Task<long> GetAppVisitsCount(DateTime from, DateTime to);
		Task<long> GetAppVisitsCount(AppReleaseType releaseType, DateTime from, DateTime to);
		Task<long> PurgeAppVisits();
		Task<bool> AddAppAction(Guid sessionId, AppAction appAction);
		Task<List<AppAction>?> GetAppActions(Guid sessionId);
		#endregion

		#region AppError
		Task AddAppError(AppError appError);
		Task<List<AppError>> GetAppErrors();
		Task<long> GetAppErrorCount();
		Task<long> PurgeAppErrors();
		#endregion
	}
}
