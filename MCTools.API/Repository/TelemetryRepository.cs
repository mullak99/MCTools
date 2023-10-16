using MCTools.SDK.Enums.Telemetry;
using MCTools.SDK.Models.Telemetry;
using MongoDB.Driver;

namespace MCTools.API.Repository
{
	public class TelemetryRepository : ITelemetryRepository
	{
		private readonly IMongoCollection<AppInfo> _appInfo;

		public TelemetryRepository(IMongoDatabase database)
		{
			_appInfo = database.GetCollection<AppInfo>("Telemetry.AppLaunchInfo");
		}

		#region AppInfo
		#region Create
		public async Task AddAppVisit(AppInfo appInfo)
			=> await _appInfo.InsertOneAsync(appInfo);
		#endregion

		#region Read
		public async Task<List<AppInfo>> GetAllAppVisits()
			=> await _appInfo.Find(_ => true).ToListAsync();

		public async Task<List<AppInfo>> GetAllAppVisits(AppReleaseType releaseType)
			=> await _appInfo.Find(x => x.ReleaseType == releaseType).ToListAsync();

		public async Task<List<AppInfo>> GetAllAppVisits(DateTime from, DateTime to)
			=> await _appInfo.Find(x => x.RequestDateTime >= from && x.RequestDateTime <= to).ToListAsync();

		public async Task<List<AppInfo>> GetAllAppVisits(AppReleaseType releaseType, DateTime from, DateTime to)
			=> await _appInfo.Find(x => x.ReleaseType == releaseType && x.RequestDateTime >= from && x.RequestDateTime <= to).ToListAsync();

		public async Task<long> GetAppVisitsCount()
			=> await _appInfo.CountDocumentsAsync(_ => true);

		public async Task<long> GetAppVisitsCount(AppReleaseType releaseType)
			=> await _appInfo.CountDocumentsAsync(x => x.ReleaseType == releaseType);

		public async Task<long> GetAppVisitsCount(DateTime from, DateTime to)
			=> await _appInfo.CountDocumentsAsync(x => x.RequestDateTime >= from && x.RequestDateTime <= to);

		public async Task<long> GetAppVisitsCount(AppReleaseType releaseType, DateTime from, DateTime to)
			=> await _appInfo.CountDocumentsAsync(x => x.ReleaseType == releaseType && x.RequestDateTime >= from && x.RequestDateTime <= to);
		#endregion

		#region Delete
		public async Task<long> DeleteAllAppVisits()
			=> (await _appInfo.DeleteManyAsync(_ => true)).DeletedCount;
		#endregion
		#endregion
	}

	public interface ITelemetryRepository
	{
		#region AppInfo
		Task AddAppVisit(AppInfo appInfo);
		Task<List<AppInfo>> GetAllAppVisits();
		Task<List<AppInfo>> GetAllAppVisits(AppReleaseType releaseType);
		Task<List<AppInfo>> GetAllAppVisits(DateTime from, DateTime to);
		Task<List<AppInfo>> GetAllAppVisits(AppReleaseType releaseType, DateTime from, DateTime to);
		Task<long> GetAppVisitsCount();
		Task<long> GetAppVisitsCount(AppReleaseType releaseType);
		Task<long> GetAppVisitsCount(DateTime from, DateTime to);
		Task<long> GetAppVisitsCount(AppReleaseType releaseType, DateTime from, DateTime to);
		Task<long> DeleteAllAppVisits();
		#endregion
	}
}
