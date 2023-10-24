using System;
using MCTools.Shared.Dialog;
using MudBlazor;

namespace MCTools.Logic
{
	public class ErrorHandler : IErrorHandler
	{
		private readonly ISnackbar _snackbar;
		private readonly IDialogService _dialogService;

		public ErrorHandler(ISnackbar snackbar, IDialogService dialogService)
		{
			_snackbar = snackbar;
			_dialogService = dialogService;
		}

		public void HandleException(Exception ex)
		{
			Console.WriteLine($"An unexpected error has occurred! Exception = {ex}");
			_snackbar.Add($"An unexpected error hasoccurred!", Severity.Error);

			DialogOptions options = new() { MaxWidth = MaxWidth.Medium, FullWidth = true };
			DialogParameters parameters = new()
			{
				{"Exception", ex}
			};
			_dialogService.Show<ErrorReportDialog>("Report Error", parameters, options);
		}
	}

	public interface IErrorHandler
	{
		void HandleException(Exception ex);
	}
}
