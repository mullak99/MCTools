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
		#region Options

		private MCVersion SelectedVersion;
		private MCEdition SelectedEdition;
		private bool IsProcessing;

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
			IsProcessing = true;

			// DownloadFromUrl causes the UI to freeze, its not the download itself since its still slow when cached.
			Console.WriteLine("Extracting and processing the JAR/ZIP can take a while. While it may appear to be stuck, it isn't. More optimisations are needed.");
			Snackbar.Add("This process can take a while. See console for more details.", Severity.Warning);

			switch (SelectedEdition)
			{
				case MCEdition.Java:
					Task<string> jarDownloadTask = _apiController.GetJavaJar(SelectedVersion.Id);
					Task<MCAssets> assetsTask = _apiController.GetJavaAssets(SelectedVersion.Id);
					await Task.WhenAll(jarDownloadTask, assetsTask);

					string jarDownload = jarDownloadTask.Result;
					MCAssets assets = assetsTask.Result;

					if (IncludeMcMetas)
						assets.Textures.AddRange(assets.Textures.Select(x => x.Replace(".png", ".png.mcmeta")).ToList()); // Support for .mcmeta files

					if (string.IsNullOrWhiteSpace(jarDownload))
						return;

					if (OutputSourceUrl)
						Console.WriteLine($"Client JAR: {jarDownload}");

					if (DownloadRawJar)
						await _jsHelper.OpenLinkInNewTab(jarDownload);
					else
						await DownloadFromUrl(jarDownload, assets);
					break;
				case MCEdition.Bedrock:
					string zipUrl = SelectedVersion.Url;
					if (string.IsNullOrWhiteSpace(zipUrl))
						return;

					if (OutputSourceUrl)
						Console.WriteLine($"Bedrock Assets: {zipUrl}");

					await _jsHelper.OpenLinkInNewTab(zipUrl); // Don't filter Bedrock assets: The size of the original ZIP would make this slow.
					break;
			}

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
				ConcurrentDictionary<string, byte[]> extractedFiles = new();
				using MemoryStream zipStream = new(zipBytes);
				using ZipFile archive = new(zipStream);

				foreach (ZipEntry entry in archive)
				{
					if (!assets.Textures.Contains(entry.Name)) continue;

					using MemoryStream ms = new();
					await using Stream entryStream = archive.GetInputStream(entry);
					await entryStream.CopyToAsync(ms).ConfigureAwait(false);
					extractedFiles.TryAdd(entry.Name, ms.ToArray());
					await Task.Delay(1); // Yield to the UI thread
				}

				if (PerfLogging)
				{
					Console.WriteLine($"Extracted assets in {perfLogging.ElapsedMilliseconds}ms");
					perfLogging.Restart();
				}

				// Zip up the extracted files and download them to the browser
				if (extractedFiles.Count > 0)
				{
					byte[] zippedBytes;
					using (MemoryStream ms = new())
					{
						await using (ZipOutputStream finalArchive = new(ms))
						{
							foreach (KeyValuePair<string, byte[]> entry in extractedFiles)
							{
								ZipEntry zipEntry = new(entry.Key);
								await finalArchive.PutNextEntryAsync(zipEntry).ConfigureAwait(false);
								finalArchive.Write(entry.Value, 0, entry.Value.Length);
								finalArchive.CloseEntry();
								await Task.Delay(1); // Yield to the UI thread
							}
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
