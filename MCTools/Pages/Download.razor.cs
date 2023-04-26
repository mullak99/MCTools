using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MCTools.Enums;
using MCTools.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MCTools.Pages
{
	public partial class Download : LayoutComponentBase
	{
		#region Variables

		#region Constants
		#endregion

		#region API Values
		private List<MCVersion> MinecraftVersions { get; set; } = new();
		private MCVersion LatestVersion { get; set; }
		#endregion

		#region Options
		private MCVersion SelectedVersion { get; set; }
		private MCEdition SelectedEdition { get; set; } = MCEdition.Java;

		private bool IsProcessing;
		#endregion

		private bool PerfLogging { get; set; }
		#endregion

		#region Blazor Overrides
		protected override async Task OnInitializedAsync()
		{
			await SelectedEditionChanged(SelectedEdition);
			StateHasChanged();
		}
		#endregion

		#region Selection
		public void SetDefaultVersionSelection()
		{
			if (MinecraftVersions is { Count: > 0 })
			{
				LatestVersion = MinecraftVersions.First(x => x.Type == "release");
				SelectedVersion = LatestVersion;
			}
			else Snackbar.Add("Unable to fetch versions! Is the API down?", Severity.Error);
		}

		public async Task SelectedEditionChanged(MCEdition edition)
		{
			if (edition != SelectedEdition || MinecraftVersions.Count == 0)
			{
				try
				{
					IsProcessing = true;
					SelectedEdition = edition;
					MinecraftVersions = new List<MCVersion>(); // Reset list

					MinecraftVersions = edition == MCEdition.Java
						? await _apiController.GetJavaVersions()
						: await _apiController.GetBedrockVersions();

					SetDefaultVersionSelection();
				}
				catch (Exception)
				{
					Snackbar.Add("An error occurred when loading versions! Check the console for errors.", Severity.Error);
					throw;
				}
				IsProcessing = false;
			}
		}
		#endregion

		#region Operations
		#region UI Buttons
		private async Task DownloadAssets()
		{
			IsProcessing = true;

			// DownloadFromUrl causes the UI to freeze, its not the download itself since its still slow when cached.
			Console.WriteLine("Extracting the JAR/ZIP for the specified assets and zipping those up freezes the web app, despite it being done on a background thread. More investigation needed.");
			Snackbar.Add("The Web App may become unresponsive for a moment. See console for more details.", Severity.Warning);

			await Task.Run(async () =>
			{
				switch (SelectedEdition)
				{
					case MCEdition.Java:
						Task<string> jarDownloadTask = _apiController.GetJavaJar(SelectedVersion.Id);
						Task<MCAssets> assetsTask = _apiController.GetJavaAssets(SelectedVersion.Id);
						await Task.WhenAll(jarDownloadTask, assetsTask);

						string jarDownload = jarDownloadTask.Result;
						MCAssets assets = assetsTask.Result;

						if (string.IsNullOrWhiteSpace(jarDownload))
							return;
						await DownloadFromUrl(jarDownload, assets);
						break;
					case MCEdition.Bedrock:
						throw new NotImplementedException();
				}
			}).ConfigureAwait(false);

			IsProcessing = false;
		}

		private async Task DownloadFromUrl(string url, MCAssets assets)
		{
			byte[] zipBytes = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);

			// Extract the relevant files from the ZIP/JAR
			Stopwatch perfLogging = new Stopwatch();

			if (PerfLogging)
				perfLogging.Start();

			var extractedFiles = new ConcurrentDictionary<string, byte[]>();
			using var zipStream = new MemoryStream(zipBytes);
			using var archive = new ZipFile(zipStream);

			var tasks = new List<Task>();

			foreach (ZipEntry entry in archive)
			{
				if (!assets.Textures.Contains(entry.Name)) continue;

				tasks.Add(Task.Run(async () =>
				{
					using var ms = new MemoryStream();
					await using var entryStream = archive.GetInputStream(entry);
					await entryStream.CopyToAsync(ms).ConfigureAwait(false);
					extractedFiles.TryAdd(entry.Name, ms.ToArray());
				}));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);

			if (PerfLogging)
			{
				Console.WriteLine($"Extracted assets in {perfLogging.ElapsedMilliseconds}ms");
				perfLogging.Restart();
			}

			// Zip up the extracted files and download them to the browser
			if (extractedFiles.Count > 0)
			{
				byte[] zippedBytes;
				using (var ms = new MemoryStream())
				{
					await using (var finalArchive = new ZipOutputStream(ms))
					{
						tasks.Clear();

						foreach (var entry in extractedFiles)
						{
							tasks.Add(Task.Run(async () =>
							{
								var zipEntry = new ZipEntry(entry.Key);
								await finalArchive.PutNextEntryAsync(zipEntry).ConfigureAwait(false);
								finalArchive.Write(entry.Value, 0, entry.Value.Length);
								finalArchive.CloseEntry();
							}));
						}

						await Task.WhenAll(tasks).ConfigureAwait(false);
					}
					zippedBytes = ms.ToArray();
				}

				if (PerfLogging)
				{
					Console.WriteLine($"Created final zip in {perfLogging.ElapsedMilliseconds}ms");
					perfLogging.Restart();
				}

				await _jsHelper.DownloadZip($"Minecraft-{(SelectedEdition == MCEdition.Java ? "Java" : "Bedrock")}-{assets.Minecraft.Version}.zip", zippedBytes);

				if (PerfLogging)
				{
					perfLogging.Stop();
					Console.WriteLine($"Download to browser in {perfLogging.ElapsedMilliseconds}ms");
				}
			}
		}
		#endregion
		#endregion
	}
}
