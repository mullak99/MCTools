using MCTools.SDK.Enums.Controllers;

namespace MCTools.SDK.Models
{
	public class MCToolsHealthStatus
	{
		public int? StatusCode
		{
			get
			{
				switch (Api.StatusCode)
				{
					case null when Database.StatusCode == null:
						return null;
					case 503:
						return 503;
				}
				if (Api.StatusCode == 206 || Database.StatusCode == 503)
					return 206;
				return 200;
			}
		}
		public Status Status
		{
			get
			{
				switch (Api.Status)
				{
					case Status.Unknown when Database.Status == Status.Unknown:
						return Status.Unknown;
					case Status.Unhealthy:
						return Status.Unhealthy;
				}
				if (Api.Status == Status.Degraded || Database.Status == Status.Unhealthy)
					return Status.Degraded;
				return Status.Healthy;
			}
		}
		public ApiStatus Api { get; set; } = new();
		public DatabaseStatus Database { get; set; } = new();

		public static MCToolsHealthStatus Unknown => new()
		{
			Api = new ApiStatus
			{
				StatusCode = null,
				Status = Status.Unknown
			},
			Database = new DatabaseStatus
			{
				StatusCode = null,
				Status = Status.Unknown
			}
		};

		public static MCToolsHealthStatus Unhealthy => new()
		{
			Api = new ApiStatus
			{
				StatusCode = 503,
				Status = Status.Unhealthy
			},
			Database = new DatabaseStatus
			{
				StatusCode = 503,
				Status = Status.Unhealthy
			}
		};
	}

	public class ApiStatus : IMCToolsHealthStatus
	{
		public int? StatusCode { get; set; }
		public Status Status { get; set; }
	}

	public class DatabaseStatus : IMCToolsHealthStatus
	{
		public int? StatusCode { get; set; }
		public Status Status { get; set; }
	}

	public interface IMCToolsHealthStatus
	{
		public int? StatusCode { get; set; }
		public Status Status { get; set; }
	}
}
