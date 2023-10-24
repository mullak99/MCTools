using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Threading.Tasks;
using MCTools.SDK.Controllers;
using MCTools.SDK.Models.Telemetry;

namespace MCTools.Shared.Dialog
{
	public partial class ErrorReportDialog : LayoutComponentBase
	{
		[CascadingParameter] MudDialogInstance MudDialog { get; set; }

		[Parameter] public Exception Exception { get; set; }

		[Inject] private TelemetryController TelemetryController { get; set; }

		private Guid _errorId;

		protected override void OnInitialized()
		{
			_errorId = Guid.NewGuid();
		}

		private async Task Submit()
		{
			AppError appError = new()
			{
				Id = _errorId,
				AppInfo = Program.GetAppInfo(),
				ExceptionType = Exception.GetType().Name,
				ExceptionMessage = Exception.Message,
				ExceptionStackTrace = Exception?.StackTrace ?? "UNKNOWN"
			};

			await TelemetryController.AddAppError(appError);
			MudDialog.Close(DialogResult.Ok(true));
		}

		private void Cancel()
			=> MudDialog.Cancel();
	}
}
