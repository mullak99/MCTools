using System;
using System.Threading.Tasks;
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

		public async Task HandleException(Exception ex)
		{
			Console.WriteLine($"An unexpected error has occurred! Exception = {ex}");
			_snackbar.Add($"An unexpected error has occurred!", Severity.Error);

			DialogOptions options = new() { MaxWidth = MaxWidth.Medium, FullWidth = true };
			DialogParameters parameters = new()
			{
				{"Exception", ex}
			};
			await _dialogService.ShowAsync<ErrorReportDialog>("Report Error", parameters, options);
		}
	}

	public interface IErrorHandler
	{
		Task HandleException(Exception ex);
	}
}
