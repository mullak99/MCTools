using MCTools.API.Logic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MCTools.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public partial class TelemetryController : ControllerBase
	{
		private readonly ITelemetryLogic _telemetryLogic;
		private readonly ILogger<TelemetryController> _logger;

		public TelemetryController(ITelemetryLogic telemetryLogic, ILogger<TelemetryController> logger)
		{
			_telemetryLogic = telemetryLogic;
			_logger = logger;
		}
	}
}
