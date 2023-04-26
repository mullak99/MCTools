using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
		#region Options

		private MCVersion SelectedVersion;
		private MCEdition SelectedEdition;
		private bool IsProcessing;

		private void SelectedVersionChanged(MCVersion version)
			=> SelectedVersion = version;

		private void SelectedEditionChanged(MCEdition edition)
			=> SelectedEdition = edition;
		#endregion

		private bool PerfLogging { get; set; }
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
						string zipUrl = SelectedVersion.Url;
						if (string.IsNullOrWhiteSpace(zipUrl))
							return;
						await DownloadFromUrl(zipUrl, null); // Don't filter Bedrock assets: The size of the original ZIP would make this slow.
						break;
				}
			}).ConfigureAwait(false);

			IsProcessing = false;
		}

		private async Task DownloadFromUrl(string url, MCAssets assets)
		{
			byte[] zipBytes = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
			Stopwatch perfLogging = new();

			if (PerfLogging)
				perfLogging.Start();

			// Filter down the assets based on the MCAssets provided
			if (assets != null)
			{
				// Extract the relevant files from the ZIP/JAR
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
			else
			{
				await _jsHelper.DownloadZip($"Minecraft-{(SelectedEdition == MCEdition.Java ? "Java" : "Bedrock")}-{assets.Minecraft.Version}.zip", zipBytes);

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
