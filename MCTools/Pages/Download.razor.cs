using ICSharpCode.SharpZipLib.Zip;
using MCTools.Enums;
using MCTools.Logic;
using MCTools.SDK.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MCTools.SDK.Models.Telemetry;

namespace MCTools.Pages
{
	public partial class Download : LayoutComponentBase
	{
		#region Variables
		#region Options

		private MCVersion SelectedVersion;
		private MCEdition SelectedEdition;
		private bool IsProcessing;
		private byte? ProgressValue;
		private string ProgressText;

		private void SelectedVersionChanged(MCVersion version)
			=> SelectedVersion = version;

		private void SelectedEditionChanged(MCEdition edition)
			=> SelectedEdition = edition;
		#endregion

		private bool IncludeMcMetas { get; set; } = true;

		private bool OutputSourceUrl { get; set; }
		private bool DownloadRawJar { get; set; }
		private bool PerfLogging { get; set; }
		#endregion

		#region Operations
		#region UI Buttons
		private async Task DownloadAssets()
		{
			try
			{
				IsProcessing = true;
				SetProgress(null, string.Empty);

				_ = TelemetryController.AddAppAction(Program.GetSessionId(), new AppAction
				{
					Action = "DownloadVanillaAssets",
					Details =
					[
						$"Edition: {(SelectedEdition == MCEdition.Java ? "Java" : "Bedrock")}",
						$"Version: {SelectedVersion.Id}"
					]
				});

				switch (SelectedEdition)
				{
					case MCEdition.Java:

						SetProgress(5, "Getting JAR url and asset list..");

						string jarDownload = await JavaController.GetJar(SelectedVersion.Id);
						MCAssets assets = await JavaController.GetAssets(SelectedVersion.Id);

						if (string.IsNullOrWhiteSpace(jarDownload))
							return;

						if (OutputSourceUrl)
							Console.WriteLine($"Client JAR: {jarDownload}");

						if (DownloadRawJar)
						{
							SetProgress(100, string.Empty);
							await JsHelper.OpenLinkInNewTab(jarDownload);
						}
						else
							await DownloadFromUrl(jarDownload, assets);

						break;
					case MCEdition.Bedrock:
						SetProgress(null, "Downloading Bedrock Assets...");

						string zipUrl = SelectedVersion.Url;
						if (string.IsNullOrWhiteSpace(zipUrl))
							return;

						if (OutputSourceUrl)
							Console.WriteLine($"Bedrock Assets: {zipUrl}");

						SetProgress(100, string.Empty);
						await JsHelper.OpenLinkInNewTab(zipUrl); // Don't filter Bedrock assets: The size of the original ZIP would make this slow.
						break;
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException(ex);
			}
			finally
			{
				IsProcessing = false;
			}
		}

		private async Task DownloadFromUrl(string url, MCAssets assets)
		{
			SetProgress(null, "Downloading JAR...");
			byte[] zipBytes = await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
			Stopwatch perfLogging = new();

			if (PerfLogging)
				perfLogging.Start();

			// Filter down the assets based on the MCAssets provided
			if (assets != null)
			{
				SetProgress(null, "Extracting JAR...");

				// Extract the relevant files from the ZIP/JAR
				ConcurrentDictionary<string, byte[]> extractedFiles = new();
				using MemoryStream zipStream = new(zipBytes);
				using ZipFile archive = new(zipStream);

				int delayInterval = 50;
				int currInterval = 0;
				foreach (ZipEntry entry in archive)
				{
					if (!assets.Textures.Contains(entry.Name) && (!IncludeMcMetas || !assets.McMetas.Contains(entry.Name))) continue;

					using MemoryStream ms = new();
					await using Stream entryStream = archive.GetInputStream(entry);
					await entryStream.CopyToAsync(ms).ConfigureAwait(false);
					extractedFiles.TryAdd(entry.Name, ms.ToArray());

					currInterval += 1;
					if (currInterval < delayInterval)
						continue;

					await Task.Delay(1); // Yield to the UI thread
					currInterval = 0;
				}

				if (PerfLogging)
				{
					Console.WriteLine($"Extracted assets in {perfLogging.ElapsedMilliseconds}ms");
					perfLogging.Restart();
				}

				SetProgress(null, "Zipping up assets...");

				// Zip up the extracted files and download them to the browser
				if (!extractedFiles.IsEmpty)
				{
					byte[] zippedBytes;
					using (MemoryStream ms = new())
					{
						await using (ZipOutputStream finalArchive = new(ms))
						{
							delayInterval = 100;
							currInterval = 0;
							foreach (KeyValuePair<string, byte[]> entry in extractedFiles)
							{
								ZipEntry zipEntry = new(entry.Key);
								await finalArchive.PutNextEntryAsync(zipEntry).ConfigureAwait(false);
								finalArchive.Write(entry.Value, 0, entry.Value.Length);
								finalArchive.CloseEntry();

								currInterval += 1;
								if (currInterval < delayInterval)
									continue;

								await Task.Delay(1); // Yield to the UI thread
								currInterval = 0;
							}
						}
						zippedBytes = ms.ToArray();
					}

					if (PerfLogging)
					{
						Console.WriteLine($"Created final zip in {perfLogging.ElapsedMilliseconds}ms");
						perfLogging.Restart();
					}

					SetProgress(95, "Downloading assets...");
					await JsHelper.DownloadZip($"Minecraft-{(SelectedEdition == MCEdition.Java ? "Java" : "Bedrock")}-{assets.Minecraft.Version}.zip", zippedBytes);
					SetProgress(100, string.Empty);

					if (PerfLogging)
					{
						perfLogging.Stop();
						Console.WriteLine($"Download to browser in {perfLogging.ElapsedMilliseconds}ms");
					}
				}
			}
			else
			{
				SetProgress(95, "Downloading assets...");
				await JsHelper.DownloadZip($"Minecraft-{(SelectedEdition == MCEdition.Java ? "Java" : "Bedrock")}-{assets.Minecraft.Version}.zip", zipBytes);
				SetProgress(100, string.Empty);

				if (PerfLogging)
				{
					perfLogging.Stop();
					Console.WriteLine($"Download to browser in {perfLogging.ElapsedMilliseconds}ms");
				}
			}
		}

		private void SetProgress(byte? value, string text)
		{
			ProgressValue = value;
			ProgressText = text;
			StateHasChanged();
		}
		#endregion
		#endregion
	}
}
