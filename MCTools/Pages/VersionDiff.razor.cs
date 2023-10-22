using ICSharpCode.SharpZipLib.Zip;
using MCTools.Enums;
using MCTools.SDK.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MCTools.Pages
{
	public partial class VersionDiff : LayoutComponentBase
	{
		#region Variables
		#region Options

		private MCVersion SelectedVersionFrom;
		private MCVersion SelectedVersionTo;
		private MCEdition SelectedEdition;
		private bool IsProcessing;
		private byte? ProgressValue;
		private string ProgressText;

		private List<string> SameAssets { get; set; } = new();
		private List<string> DifferentAssets { get; set; } = new();
		private List<string> RemovedAssets { get; set; } = new();
		private List<string> AddedAssets { get; set; } = new();

		private ConcurrentDictionary<string, byte[]> FromAssets;
		private ConcurrentDictionary<string, byte[]> ToAssets;

		private MCVersion SavedFromVersion;
		private MCVersion SavedToVersion;

		private bool _compareEnabled => IsProcessing || SelectedVersionFrom == SelectedVersionTo;

		private void SelectedVersionChanged(MCVersion from, MCVersion to)
		{
			SelectedVersionFrom = from;
			SelectedVersionTo = to;
		}

		private void SelectedEditionChanged(MCEdition edition)
			=> SelectedEdition = edition;
		#endregion

		private bool IncludeMcMetas { get; set; } = true;

		private bool OutputSourceUrl { get; set; }
		private bool DownloadRawJar { get; set; }
		private bool PerfLogging { get; set; }
		private bool DebugLogging { get; set; }
		#endregion

		#region Operations
		#region UI Buttons

		private void Reset()
		{
			SameAssets = new();
			DifferentAssets = new();
			RemovedAssets = new();
			AddedAssets = new();

			FromAssets = null;
			ToAssets = null;

			SavedFromVersion = SelectedVersionFrom;
			SavedToVersion = SelectedVersionTo;

			SetProgress(null, string.Empty);
		}

		private async Task CompareAssets()
		{
			IsProcessing = true;
			Reset();

			// DownloadFromUrl causes the UI to freeze, its not the download itself since its still slow when cached.
			Console.WriteLine("Extracting and processing the JAR/ZIP can take a while. While it may appear to be stuck, it isn't. More optimisations are needed.");
			Snackbar.Add("This process can take a while. See console for more details.", Severity.Warning);

			switch (SelectedEdition)
			{
				case MCEdition.Java:
					Task<string> jarFromDownloadTask = JavaController.GetJar(SelectedVersionFrom.Id);
					Task<MCAssets> assetsFromTask = JavaController.GetAssets(SelectedVersionFrom.Id);

					Task<string> jarToDownloadTask = JavaController.GetJar(SelectedVersionTo.Id);
					Task<MCAssets> assetsToTask = JavaController.GetAssets(SelectedVersionTo.Id);

					SetProgress(10, "Getting assets...");
					await Task.WhenAll(jarFromDownloadTask, assetsFromTask, jarToDownloadTask, assetsToTask);

					if (DebugLogging)
						Console.WriteLine("Got assets and download urls");

					string jarDownloadFrom = jarFromDownloadTask.Result;
					MCAssets assetsFrom = assetsFromTask.Result;

					string jarDownloadTo = jarToDownloadTask.Result;
					MCAssets assetsTo = assetsToTask.Result;

					if (IncludeMcMetas)
					{
						// Support for .mcmeta files
						assetsFrom.Textures.AddRange(assetsFrom.Textures.Select(x => x.Replace(".png", ".png.mcmeta")).ToList());
						assetsTo.Textures.AddRange(assetsTo.Textures.Select(x => x.Replace(".png", ".png.mcmeta")).ToList());

						if (DebugLogging)
							Console.WriteLine("Included mcmetas");
					}

					if (string.IsNullOrWhiteSpace(jarDownloadTo) || string.IsNullOrWhiteSpace(jarDownloadFrom))
					{
						SetProgress(100, string.Empty);
						Snackbar.Add("Unable to download JARs!", Severity.Error);
						IsProcessing = false;
						return;
					}

					if (OutputSourceUrl)
						Console.WriteLine($"Client JAR (1): {jarDownloadFrom} | Client JAR (2): {jarDownloadTo}");

					if (DownloadRawJar)
					{
						await Task.WhenAll(JsHelper.OpenLinkInNewTab(jarDownloadFrom), JsHelper.OpenLinkInNewTab(jarDownloadTo));
						SetProgress(100, string.Empty);
						IsProcessing = false;
						return;
					}

					if (DebugLogging)
						Console.WriteLine("Starting asset download...");

					SetProgress(null, "Downloading assets...");

					Task<ConcurrentDictionary<string, byte[]>> fromTask = DownloadFromUrl(jarDownloadFrom, assetsFrom, "From");
					Task<ConcurrentDictionary<string, byte[]>> toTask = DownloadFromUrl(jarDownloadTo, assetsTo, "To");
					await Task.WhenAll(fromTask, toTask);

					if (DebugLogging)
						Console.WriteLine("Downloaded assets!");

					SetProgress(85, "Downloaded assets!");

					FromAssets = fromTask.Result;
					ToAssets = toTask.Result;

					if (FromAssets == null || ToAssets == null)
					{
						Snackbar.Add("Download assets were invalid!", Severity.Error);
						SetProgress(100, string.Empty);
						IsProcessing = false;
						return;
					}

					List<string> sameFiles = new List<string>();
					List<string> differentFiles = new List<string>();
					List<string> removedFiles = new List<string>();

					if (DebugLogging)
						Console.WriteLine("Starting asset comparison...");

					SetProgress(90, "Comparing assets...");

					foreach (var item in FromAssets)
					{
						if (ToAssets.ContainsKey(item.Key))
						{
							if (ComputeHash(item.Value) == ComputeHash(ToAssets[item.Key]))
								sameFiles.Add(item.Key);
							else
							{
								// Hashes didn't match, perform pixel-by-pixel comparison
								if (AreImagesIdentical(item.Value, ToAssets[item.Key]))
									sameFiles.Add(item.Key);
								else
									differentFiles.Add(item.Key);
							}
						}
						else removedFiles.Add(item.Key);
					}
					List<string> addedFiles = (from item in ToAssets where !FromAssets.ContainsKey(item.Key) select item.Key).ToList();

					SameAssets = sameFiles;
					DifferentAssets = differentFiles;
					RemovedAssets = removedFiles;
					AddedAssets = addedFiles;

					SetProgress(100, "Done!");
					if (DebugLogging)
						Console.WriteLine("Finished!");

					break;
				case MCEdition.Bedrock:
					Snackbar.Add("Bedrock edition is not supported!", Severity.Warning);
					break;
			}
			IsProcessing = false;
		}

		private async Task<ConcurrentDictionary<string, byte[]>> DownloadFromUrl(string url, MCAssets assets, string consoleId)
		{
			byte[] zipBytes = await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
			Stopwatch perfLogging = new();

			if (PerfLogging)
				perfLogging.Start();

			if (assets == null)
			{
				Snackbar.Add("Unable to filter down the assets!", Severity.Error);
				return null;
			}

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
				Console.WriteLine($"[{consoleId}] Extracted assets in {perfLogging.ElapsedMilliseconds}ms");
				perfLogging.Stop();
			}

			// Zip up the extracted files and download them to the browser
			if (!extractedFiles.IsEmpty)
				return extractedFiles;

			Snackbar.Add("Unable to extract vanilla assets!", Severity.Error);
			return null;
		}

		private static string ComputeHash(byte[] data)
		{
			using SHA1 sha1 = SHA1.Create();
			byte[] hash = sha1.ComputeHash(data);
			StringBuilder sb = new();
			foreach (var t in hash)
				sb.Append(t.ToString("x2"));

			return sb.ToString();
		}

		private static bool AreImagesIdentical(byte[] image1Data, byte[] image2Data)
		{
			using var ms1 = new MemoryStream(image1Data);
			using var ms2 = new MemoryStream(image2Data);
			using var image1 = Image.Load<Rgba32>(ms1);
			using var image2 = Image.Load<Rgba32>(ms2);

			if (image1.Width != image2.Width || image1.Height != image2.Height)
				return false;

			for (int y = 0; y < image1.Height; y++)
			{
				for (int x = 0; x < image1.Width; x++)
				{
					if (!image1[x, y].Equals(image2[x, y]))
						return false;
				}
			}
			return true;
		}

		private async Task DownloadAssets(string type, List<string> targetAssets, ConcurrentDictionary<string, byte[]> toAssets)
		{
			using MemoryStream ms = new();
			await using (ZipOutputStream finalArchive = new(ms))
			{
				foreach (var entry in targetAssets)
				{
					var asset = toAssets[entry];
					if (asset == null)
						continue;

					ZipEntry zipEntry = new(entry);
					await finalArchive.PutNextEntryAsync(zipEntry).ConfigureAwait(false);
					finalArchive.Write(asset, 0, asset.Length);
					finalArchive.CloseEntry();
					await Task.Delay(1); // Yield to the UI thread
				}
			}
			byte[] zippedBytes = ms.ToArray();
			await JsHelper.DownloadZip($"{type}-{SavedFromVersion.Id}-to-{SavedToVersion.Id}-{(SelectedEdition == MCEdition.Java ? "Java" : "Bedrock")}.zip", zippedBytes);
		}

		private void SetProgress(byte? value, string text)
		{
			ProgressValue = value;
			ProgressText = text;
			StateHasChanged();
		}

		private byte[] ShowDifferences(string asset)
		{
			using var ms1 = new MemoryStream(FromAssets[asset]);
			using var ms2 = new MemoryStream(ToAssets[asset]);
			using var image1 = Image.Load<Rgba32>(ms1);
			using var image2 = Image.Load<Rgba32>(ms2);

			if (image1.Width != image2.Width || image1.Height != image2.Height)
			{
				Console.WriteLine($"Image sizes are different for {asset}! {image1.Width}x{image1.Height} vs {image2.Width}x{image2.Height}");
				return null;
			}

			var diffImage = new Image<Rgba32>(image1.Width, image1.Height);

			for (int y = 0; y < image1.Height; y++)
			{
				for (int x = 0; x < image1.Width; x++)
				{
					var pixel1 = image1[x, y];
					var pixel2 = image2[x, y];

					if (pixel1.Equals(pixel2))
					{
						// Identical
						diffImage[x, y] = new Rgba32(0, 0, 255, 255);
					}
					else
					{
						// Compute difference magnitude
						float diff = (Math.Abs(pixel1.R - pixel2.R) + Math.Abs(pixel1.G - pixel2.G) + Math.Abs(pixel1.B - pixel2.B)) / 3.0f;
						float scale = diff / 255.0f;
						diffImage[x, y] = new Rgba32(255, 0, 255, (byte)(scale * 255));
					}
				}
			}

			using var ms = new MemoryStream();
			diffImage.SaveAsPng(ms);
			return ms.ToArray();
		}

		private async Task DownloadAddedAssets()
			=> await DownloadAssets("Added", AddedAssets, ToAssets);

		private async Task DownloadRemovedAssets()
			=> await DownloadAssets("Removed", RemovedAssets, ToAssets);

		private async Task DownloadDifferentAssets()
			=> await DownloadAssets("Changed", DifferentAssets, ToAssets);

		private async Task DownloadSameAssets()
			=> await DownloadAssets("Unchanged", SameAssets, ToAssets);

		private async Task DownloadDifferentAssetsShowDiff()
		{
			Dictionary<string, byte[]> diffAssets = DifferentAssets.ToDictionary(asset => asset, ShowDifferences);
			bool didDetectError = false;

			using MemoryStream ms = new();
			await using (ZipOutputStream finalArchive = new(ms))
			{
				foreach (var entry in diffAssets)
				{
					if (entry.Value == null)
					{
						didDetectError = true;
						continue;
					}

					ZipEntry zipEntry = new(entry.Key);
					await finalArchive.PutNextEntryAsync(zipEntry).ConfigureAwait(false);
					finalArchive.Write(entry.Value, 0, entry.Value.Length);
					finalArchive.CloseEntry();
					await Task.Delay(1); // Yield to the UI thread
				}

				StringBuilder sb = new();

				sb.AppendLine("This archive contains the differences between the two selected versions.\nPixel Colour Key:");
				sb.AppendLine("- Blue: Pixels that are unchanged between From and To.");
				sb.AppendLine("- Magenta: Pixels that are different. The shade shows the magnitude of the difference.");

				if (didDetectError)
				{
					Snackbar.Add("Unable to show differences for some assets! Check console for more information", Severity.Warning);

					sb.AppendLine();
					sb.AppendLine("Warning! Unable to show differences for some assets!\n");
					foreach (var entry in diffAssets.Where(x => x.Value == null))
						sb.AppendLine($"- {entry.Key}");
				}

				byte[] infoBytes = Encoding.UTF8.GetBytes(sb.ToString());
				ZipEntry textEntry = new("README.txt");
				await finalArchive.PutNextEntryAsync(textEntry).ConfigureAwait(false);
				finalArchive.Write(infoBytes, 0, infoBytes.Length);
				finalArchive.CloseEntry();
			}

			byte[] zippedBytes = ms.ToArray();
			await JsHelper.DownloadZip($"Changed_Highlighted-{SavedFromVersion.Id}-to-{SavedToVersion.Id}-{(SelectedEdition == MCEdition.Java ? "Java" : "Bedrock")}.zip", zippedBytes);
		}

		#endregion
		#endregion
	}
}
