using ICSharpCode.SharpZipLib.Zip;
using MCTools.Enums;
using MCTools.Logic;
using MCTools.Models;
using MCTools.SDK.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace MCTools.Pages
{
	public partial class PotionConverter : LayoutComponentBase
	{
		#region Variables

		private static readonly string[] REQUIRED_FILES = {
			"potion.png", "splash_potion.png", "lingering_potion.png", "potion_overlay.png", "tipped_arrow_base.png", "tipped_arrow_head.png"
		};
		private const string PACK_PATH = @"assets\minecraft\textures\item";

		#region Options
		private string UploadText => "Upload Resource Pack" + (Pack != null ? $": {Pack.Name}" : "");

		private MCVersion SelectedVersion;
		private MCEdition SelectedEdition;
		private bool IsProcessing;
		private bool IsOverlaySupported;

		private ResourcePack Pack { get; set; }

		private async Task SelectedVersionChanged(MCVersion version)
		{
			SelectedVersion = version;
			IsOverlaySupported = await GetOverlaySupport();
		}

		private void SelectedEditionChanged(MCEdition edition)
			=> SelectedEdition = edition;
		#endregion

		private bool DebugLogging { get; set; }
		private bool PerfLogging { get; set; }
		#endregion

		#region Operations
		#region UI Buttons
		private async Task UploadFile(InputFileChangeEventArgs e)
		{
			try
			{
				IsProcessing = true;

				List<string> errors = Validation.PackValidation(SelectedEdition, e.File);
				if (errors.Count > 0) // Show warnings for any validation errors
				{
					errors.ForEach(x => Snackbar.Add(x, Severity.Warning));
					Pack = null;
				}
				else // Pack is valid
				{
					Snackbar.Add("Resource pack uploaded!", Severity.Success);
					Pack = new(e.File, SelectedEdition);
					await Pack.Init(Validation.GetMaxFileSizeBytes());
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException(ex);
			}
			finally
			{
				IsProcessing = false;
				await InvokeAsync(StateHasChanged);
			}
		}

		private async Task ConvertAssets()
		{
			try
			{
				IsProcessing = true;

				bool success = await Pack.Process(Validation.GetMaxFileSizeBytes());
				if (!success)
				{
					Snackbar.Add("Unable to process resource pack!", Severity.Error);
					return;
				}

				List<EffectItem> effectItems = await ConversionController.GetAllEffectItems();
				if (effectItems.Count == 0)
				{
					Snackbar.Add("Unable to get effect items!", Severity.Error);
					return;
				}

				Stopwatch fileCheckSw = Stopwatch.StartNew();
				List<string> reqPaths = new();
				foreach (string file in REQUIRED_FILES)
				{
					string path = Path.Combine(PACK_PATH, file).Replace('\\', '/');
					string texturePath = Pack.GetTexturePath(path);
					if (!string.IsNullOrWhiteSpace(texturePath))
					{
						reqPaths.Add(texturePath);

						if (DebugLogging)
							Console.WriteLine($"Found file: {texturePath}");
					}
				}
				fileCheckSw.Stop();
				if (PerfLogging)
					Console.WriteLine($"File check took {fileCheckSw.ElapsedMilliseconds}ms");

				// Potion textures
				Image<Rgba32> potionBottle = null;
				Image<Rgba32> splashPotionBottle = null;
				Image<Rgba32> lingeringPotionBottle = null;
				Image<Rgba32> potionOverlay = null;

				// Tipped arrow textures
				Image<Rgba32> tippedArrowBase = null;
				Image<Rgba32> tippedArrowOverlay = null;

				Stopwatch zipReadSw = Stopwatch.StartNew();
				Stream browserStream = Pack.GetFile().OpenReadStream(Validation.GetMaxFileSizeBytes());
				MemoryStream zipMemoryStream = new();
				await browserStream.CopyToAsync(zipMemoryStream);

				// Reset the position so the stream can be read again.
				zipMemoryStream.Position = 0;

				zipReadSw.Stop();
				if (PerfLogging)
					Console.WriteLine($"Reading zip file into memory took {zipReadSw.ElapsedMilliseconds}ms");

				Stopwatch readZipSw = Stopwatch.StartNew();
				using (ZipFile zipFile = new(zipMemoryStream))
				{
					foreach (ZipEntry entry in zipFile)
					{
						if (!reqPaths.Contains(entry.Name))
							continue;

						using MemoryStream entryMemoryStream = new();
						await using (Stream zipStream = zipFile.GetInputStream(entry))
						{
							await zipStream.CopyToAsync(entryMemoryStream);
						}

						entryMemoryStream.Position = 0;
						byte[] imageBytes = entryMemoryStream.ToArray();

						if (entry.Name.EndsWith("/potion.png"))
							potionBottle = Image.Load<Rgba32>(imageBytes);
						else if (entry.Name.EndsWith("/splash_potion.png"))
							splashPotionBottle = Image.Load<Rgba32>(imageBytes);
						else if (entry.Name.EndsWith("/lingering_potion.png"))
							lingeringPotionBottle = Image.Load<Rgba32>(imageBytes);
						else if (entry.Name.EndsWith("/potion_overlay.png"))
							potionOverlay = Image.Load<Rgba32>(imageBytes);
						else if (entry.Name.EndsWith("/tipped_arrow_base.png"))
							tippedArrowBase = Image.Load<Rgba32>(imageBytes);
						else if (entry.Name.EndsWith("/tipped_arrow_head.png"))
							tippedArrowOverlay = Image.Load<Rgba32>(imageBytes);
					}
				}
				readZipSw.Stop();
				if (PerfLogging)
					Console.WriteLine($"Processing zip file took {readZipSw.ElapsedMilliseconds}ms");

				if (potionBottle == null || splashPotionBottle == null || lingeringPotionBottle == null || potionOverlay == null || tippedArrowBase == null || tippedArrowOverlay == null)
				{
					Snackbar.Add("Missing required textures!", Severity.Error);
					return;
				}

				Stopwatch zipWriteSw = Stopwatch.StartNew();
				using (MemoryStream memoryStream = new())
				{
					await using (ZipOutputStream zipOutputStream = new(memoryStream))
					{
						effectItems.ForEach(async item => await CreateEffectItem(item, potionBottle, splashPotionBottle, lingeringPotionBottle, tippedArrowBase, potionOverlay, tippedArrowOverlay, zipOutputStream));

						if (DebugLogging)
							Console.WriteLine("Copying base textures");

						await CopyTexture(potionBottle, "potion_bottle_empty.png", zipOutputStream);
						await CopyTexture(lingeringPotionBottle, "potion_bottle_lingering_empty.png", zipOutputStream);
						await CopyTexture(tippedArrowBase, "tipped_arrow_base.png", zipOutputStream);
						await CopyTexture(tippedArrowOverlay, "tipped_arrow_head.png", zipOutputStream);
					}
					byte[] zipBytes = memoryStream.ToArray();
					MCEdition convertedToEdition = SelectedEdition == MCEdition.Bedrock ? MCEdition.Java : MCEdition.Bedrock;
					await JsHelper.DownloadZip($"Potions-{convertedToEdition}-{Path.GetFileNameWithoutExtension(Pack.Name)}.zip", zipBytes);
				}
				zipWriteSw.Stop();
				if (PerfLogging)
					Console.WriteLine($"Writing zip file took {zipWriteSw.ElapsedMilliseconds}ms");
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

		private async Task CopyTexture(Image source, string destination, ZipOutputStream zipOutputStream)
		{
			using MemoryStream pngStream = new();
			await source.SaveAsPngAsync(pngStream);
			await AddEntryToZipFileAsync(zipOutputStream, destination, pngStream);
		}

		private async Task AddEntryToZipFileAsync(ZipOutputStream zipOutputStream, string entryName, Stream data)
		{
			data.Position = 0; // Reset the stream position to the beginning

			ZipEntry entry = new(entryName)
			{
				DateTime = DateTime.Now,
				Size = data.Length
			};
			await zipOutputStream.PutNextEntryAsync(entry);
			await data.CopyToAsync(zipOutputStream);  // Asynchronous copy

			zipOutputStream.CloseEntry();  // Close the current entry
		}

		private async Task CreateEffectItem(EffectItem potion, Image potionImg, Image splashImg, Image lingeringImg, Image tippedArrowBaseImg, Image<Rgba32> potionOverlayImage, Image<Rgba32> tippedArrowOverlayImage, ZipOutputStream zipOutputStream)
		{
			if (potion.PotionName != null)
			{
				if (DebugLogging)
					Console.WriteLine($"Creating potion: {potion.Name} (P: {!potion.DisablePotion} | S: {!potion.DisableSplashPotion} | L: {!potion.DisableLingeringPotion})");

				Image<Rgba32> potionOverlay = CreateOverlayTexture(potionOverlayImage, potion.GetColour());

				if (!potion.DisablePotion)
					await ApplyOverlayTexture(potionImg, potionOverlay, potion.GetPotionName()!, zipOutputStream);

				if (!potion.DisableSplashPotion)
					await ApplyOverlayTexture(splashImg, potionOverlay, potion.GetSplashPotionName()!, zipOutputStream);

				if (!potion.DisableLingeringPotion)
					await ApplyOverlayTexture(lingeringImg, potionOverlay, potion.GetLingeringPotionName()!, zipOutputStream);
			}

			if (potion.TippedArrowName != null)
			{
				if (DebugLogging)
					Console.WriteLine($"Creating tipped arrow: {potion.Name}");

				Image<Rgba32> tippedArrowOverlay = CreateOverlayTexture(tippedArrowOverlayImage, potion.GetColour());

				await ApplyOverlayTexture(tippedArrowBaseImg, tippedArrowOverlay, potion.GetTippedArrowName()!, zipOutputStream);
			}
		}

		private Image<Rgba32> CreateOverlayTexture(Image<Rgba32> overlayImage, int? tintColor)
		{
			Image<Rgba32> overlay = overlayImage.Clone();
			if (tintColor == null)
				return overlay;

			int red = (tintColor.Value >> 16) & 0xFF;
			int green = (tintColor.Value >> 8) & 0xFF;
			int blue = (tintColor.Value >> 0) & 0xFF;

			overlay.Mutate(_ =>
			{
				Memory<Rgba32> pixelBuffer = overlay.GetPixelMemoryGroup().Single();
				Span<Rgba32> pixelSpan = pixelBuffer.Span;
				for (int i = 0; i < pixelSpan.Length; i++)
				{
					Rgba32 pixel = pixelSpan[i];
					pixel.R = (byte)Math.Floor(pixel.R * red / 255.0f);
					pixel.G = (byte)Math.Floor(pixel.G * green / 255.0f);
					pixel.B = (byte)Math.Floor(pixel.B * blue / 255.0f);
					pixelSpan[i] = pixel;
				}
			});
			return overlay;
		}

		private async Task ApplyOverlayTexture(Image baseImage, Image overlayImage, string finalFileName, ZipOutputStream zipOutputStream)
		{
			using Image<Rgba32> result = new(baseImage.Width, baseImage.Height);
			result.Mutate(context =>
			{
				context.DrawImage(baseImage, new Point(0, 0), 1.0f);    // Draw the base image
				context.DrawImage(overlayImage, new Point(0, 0), 1.0f); // Draw the overlay
			});

			MemoryStream ms = new();
			await result.SaveAsPngAsync(ms);
			await AddEntryToZipFileAsync(zipOutputStream, finalFileName, ms);
		}

		private async Task<bool> GetOverlaySupport()
			=> SelectedEdition == MCEdition.Java && await JavaController.GetOverlaySupport(SelectedVersion.Id);

		private string ConversionString
			=> $"Convert ({(SelectedEdition == MCEdition.Java ? "Java > Bedrock" : "Bedrock > Java")})";
		#endregion
		#endregion
	}
}
