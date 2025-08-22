using MCTools.SDK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MCTools.API.Controllers
{
	[Route("health")]
	[ApiController]
	public class HealthController : ControllerBase
	{
		private readonly HealthCheckService _healthChecks;
		private readonly ILogger<HealthController> _logger;

		public HealthController(HealthCheckService healthChecks, ILogger<HealthController> logger)
		{
			_healthChecks = healthChecks;
			_logger = logger;
		}


		[HttpGet("summary")]
		[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
		public async Task<ActionResult<MCToolsHealthStatus>> GetHealthSummary(CancellationToken cancellationToken)
		{
			var report = await _healthChecks.CheckHealthAsync(_ => true, cancellationToken);

			var apiStatus = report.Entries.TryGetValue("self", out var self)
				? self.Status : HealthStatus.Unhealthy;

			if (self.Exception != null)
				_logger.LogError(self.Exception, "Health check 'self' reported an exception");

			var dbStatus = report.Entries.TryGetValue("mongodb", out var db)
				? db.Status : HealthStatus.Unhealthy;

			if (db.Exception != null)
				_logger.LogError(db.Exception, "Health check 'mongodb' reported an exception");

			MCToolsHealthStatus status = new()
			{
				Api = new()
				{
					Status = apiStatus switch
					{
						HealthStatus.Healthy => SDK.Enums.Controllers.Status.Healthy,
						HealthStatus.Degraded => SDK.Enums.Controllers.Status.Degraded,
						_ => SDK.Enums.Controllers.Status.Unhealthy
					},
					StatusCode = apiStatus switch
					{
						HealthStatus.Healthy => StatusCodes.Status200OK,
						HealthStatus.Degraded => StatusCodes.Status206PartialContent,
						_ => StatusCodes.Status503ServiceUnavailable
					}
				},
				Database = new()
				{
					Status = dbStatus switch
					{
						HealthStatus.Healthy => SDK.Enums.Controllers.Status.Healthy,
						HealthStatus.Degraded => SDK.Enums.Controllers.Status.Degraded,
						_ => SDK.Enums.Controllers.Status.Unhealthy
					},
					StatusCode = dbStatus switch
					{
						HealthStatus.Healthy => StatusCodes.Status200OK,
						HealthStatus.Degraded => StatusCodes.Status206PartialContent,
						_ => StatusCodes.Status503ServiceUnavailable
					}
				}
			};

			return new ObjectResult(status)
			{
				StatusCode = status.StatusCode
			};
		}
	}
}
