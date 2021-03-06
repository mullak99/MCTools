using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MCTools.Enums;
using MCTools.Logic;
using MCTools.Models;
using MCTools.Shared.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Newtonsoft.Json;

namespace MCTools.Pages
{
	public partial class Textures : LayoutComponentBase
	{
		#region Variables

		#region Constants
		private const int MAX_FILESIZE_MB = 100;
		private const int MAX_FILESIZE_BYTES = MAX_FILESIZE_MB * 1024 * 1024;

		private const float VIRTUALIZER_ITEM_SIZE = 36.02f;
		private const int VIRTUALIZER_OVERSCAN = 64;

		private const string TABLE_TAB_PANEL_STYLE = "width:200px;";
		private const string TABLE_CONTENT_STYLE = "max-height: 300px; width: 100%; overflow-y: scroll;";
		private const string TABLE_COUNT_STYLE = "position: absolute; top: 4px; right: 12px; z-index: 10;";
		private const string TABLE_OPTIONS_STYLE = "position: absolute; bottom: 4px; right: 4px; z-index: 10;";
		#endregion

		#region API Values
		private List<MCVersion> MinecraftVersions { get; set; } = new();
		private MCVersion LatestVersion { get; set; }
		private MCAssets Assets { get; set; }
		#endregion

		#region Options
		private string UploadText => "Upload Resource Pack" + (File != null ? $": {File.Name}" : "");
		private MCVersion SelectedVersion { get; set; }
		private MCEdition SelectedEdition { get; set; } = MCEdition.Java;

		private bool IsProcessing;

		#if DEBUG
		private bool DebugMode = true;
		#else
		private bool DebugMode = false;
		#endif

		private bool CanCompare => File is { Size: > 0 } && SelectedVersion != null && SelectedEdition > 0;

		private bool ExcludeRealms { get; set; } = true;
		private bool ExcludeFonts { get; set; } = true;
		private bool ExcludeOptifine { get; set; } = true;
		private bool ExcludeMisc { get; set; } = true;
		private bool ExcludeBedrockUI { get; set; }

		private bool UseParallel { get; set; }
		private bool PerfLogging { get; set; }

		private List<string> BlacklistRegexJava = new();
		private List<string> BlacklistRegexBedrock = new();

		#region Defaults
		private readonly List<string> DefaultBlacklistJava = new()
			{ @"_MACOSX", @"assets\/minecraft\/textures\/ctm", @"assets\/minecraft\/textures\/custom", @"textures\/colormap", @"background\/panorama_overlay.png" };

		private readonly List<string> DefaultBlacklistBedrock = new()
			{ @"_MACOSX", @"texts\/", @"textures\/persona_thumbnails", @"textures\/colormap" };
		#endregion
		#endregion

		#region Results
		public IBrowserFile File { get; set; }

		private List<string> MatchingTexturesList = new();
		private List<string> MissingTexturesList = new();
		private List<string> UnusedTexturesList = new();

		private int TotalTextures;
		#endregion

		private JSHelper jsHelper;
		#endregion

		#region Blazor Overrides
		protected override async Task OnInitializedAsync()
		{
			jsHelper = new JSHelper(JS);
			await Task.WhenAll(SelectedEditionChanged(SelectedEdition), SetBlacklistFromLocalStorage(), GetDebugFromLocalStorage());
			StateHasChanged();
		}
		#endregion

		#region Local Storage
		/// <summary>
		/// Get debug mode status from the users local storage
		/// </summary>
		private async Task GetDebugFromLocalStorage()
		{
			try
			{
				DebugMode = await localStore.GetItemAsync<bool?>("debugMode") ?? false;
			}
			catch (Exception)
			{
				DebugMode = false;
			}
		}

		/// <summary>
		/// Set blacklists from the users local storage
		/// </summary>
		private async Task SetBlacklistFromLocalStorage()
		{
			string rawJavaBL = await localStore.GetItemAsStringAsync("blacklistJava");
			string rawBedrockBL = await localStore.GetItemAsStringAsync("blacklistBedrock");

			try
			{
				if (!string.IsNullOrWhiteSpace(rawJavaBL))
					BlacklistRegexJava = JsonConvert.DeserializeObject<List<string>>(rawJavaBL);
				else
					await ResetBlacklist(MCEdition.Java);
			}
			catch (Exception)
			{
				Console.WriteLine("LocalStorage: blacklistJava was invalid, resetting it...");
				await ResetBlacklist(MCEdition.Java);
			}

			try
			{
				if (!string.IsNullOrWhiteSpace(rawBedrockBL))
					BlacklistRegexBedrock = JsonConvert.DeserializeObject<List<string>>(rawBedrockBL);
				else
					await ResetBlacklist(MCEdition.Bedrock);
			}
			catch (Exception)
			{
				Console.WriteLine("LocalStorage: blacklistBedrock was invalid, resetting it...");
				await ResetBlacklist(MCEdition.Bedrock);
			}
		}

		/// <summary>
		/// Reset specific blacklist
		/// </summary>
		/// <param name="edition">Edition to reset</param>
		private async Task ResetBlacklist(MCEdition edition)
		{
			if (edition == MCEdition.Java)
			{
				BlacklistRegexJava = DefaultBlacklistJava;
				await localStore.SetItemAsStringAsync("blacklistJava", JsonConvert.SerializeObject(BlacklistRegexJava));
			}
			else
			{
				BlacklistRegexBedrock = DefaultBlacklistBedrock;
				await localStore.SetItemAsStringAsync("blacklistBedrock", JsonConvert.SerializeObject(BlacklistRegexJava));
			}
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
					Reset();
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

		public string GetSuffix(MCVersion version)
		{
			switch (version.Type)
			{
				case "snapshot":
					return " (Snapshot)";
				case "beta":
					return " (Beta)";
				default:
				{
					if (version == LatestVersion)
						return " (Latest)";
					break;
				}
			}
			return string.Empty;
		}
		#endregion

		#region Operations
		#region UI Buttons
		/// <summary>
		/// Compare missing/matching/unused textures of the uploaded pack against the specified edition and version
		/// </summary>
		private async Task Compare()
		{
			IsProcessing = true;
			StateHasChanged();
			List<string> tempBlackList = new List<string>();

			if (SelectedEdition == MCEdition.Java)
			{
				tempBlackList.AddRange(BlacklistRegexJava);

				if (ExcludeRealms)
					tempBlackList.Add(@"assets\/realms");
				if (ExcludeFonts)
					tempBlackList.Add(@"textures\/font");
				if (ExcludeMisc)
					tempBlackList.Add(@"textures\/misc");
				if (ExcludeOptifine)
					tempBlackList.Add(@"assets\/minecraft\/optifine");
			}
			else
			{
				tempBlackList.AddRange(BlacklistRegexBedrock);

				if (ExcludeFonts)
					tempBlackList.Add(@"font\/");
				if (ExcludeMisc)
					tempBlackList.Add(@"textures\/misc");
				if (ExcludeBedrockUI)
				{
					tempBlackList.Add(@"textures\/gui");
					tempBlackList.Add(@"textures\/ui");
				}

				// Bedrock asset generation can be slow when generated for the first time, maybe schedule the API to get them periodically?
				Snackbar.Add("Generating Bedrock assets for the first time can be slow.", Severity.Warning);
			}

			try
			{
				Assets = SelectedEdition == MCEdition.Java
					? await _apiController.GetJavaAssets(SelectedVersion.Id)
					: await _apiController.GetBedrockAssets(SelectedVersion.Id);

				if (Assets.Textures is not { Count: > 0 })
				{
					Snackbar.Add("Unable to load assets! Is the API down?", Severity.Error);
					IsProcessing = false;
					return;
				}
			}
			catch (Exception)
			{
				Snackbar.Add("An error occurred when loading assets! Check the console for errors.", Severity.Error);
				throw;
			}
			await CompareTextures((await GetPackFileList() ?? new List<string>()), Assets.Textures, tempBlackList);
			IsProcessing = false;
			StateHasChanged();
		}

		/// <summary>
		/// Reset Textures Tool UI
		/// </summary>
		private void Reset()
		{
			MatchingTexturesList = new List<string>();
			MissingTexturesList = new List<string>();
			UnusedTexturesList = new List<string>();
			File = null;
			TotalTextures = 0;
		}
		#endregion

		/// <summary>
		/// Compare uploaded packs textures against Minecraft's texture list
		/// </summary>
		/// <param name="packFiles">List of textures from the uploaded resource pack</param>
		/// <param name="refFiles">List of Minecraft official texture assets</param>
		/// <param name="blacklist">Regex statements for textures to ignore</param>
		/// <returns></returns>
		private async Task CompareTextures(List<string> packFiles, List<string> refFiles, List<string> blacklist)
		{
			// Clear texture lists
			MatchingTexturesList.Clear();
			MissingTexturesList.Clear();
			UnusedTexturesList.Clear();
			TotalTextures = 0;

			// Run through all of the reference (MC) textures
			Task refTask = Task.Run(() =>
			{
				Stopwatch st = Stopwatch.StartNew();
				if (UseParallel)
				{
					Parallel.ForEach(refFiles, (x) =>
					{
						if (!blacklist.Any(rule => Regex.IsMatch(x, rule)))
						{
							TotalTextures++;
							if (packFiles.Contains(x))
								MatchingTexturesList.Add(x); // Pack contains this texture
							else
								MissingTexturesList.Add(x); // Pack doesn't contain this texture
						}
					});
				}
				else
				{
					refFiles.ForEach(x =>
					{
						if (!blacklist.Any(rule => Regex.IsMatch(x, rule)))
						{
							TotalTextures++;
							if (packFiles.Contains(x))
								MatchingTexturesList.Add(x); // Pack contains this texture
							else
								MissingTexturesList.Add(x); // Pack doesn't contain this texture
						}
					});
				}
				st.Stop();

				if (PerfLogging)
					Console.WriteLine($"Ran through all reference textures in {st.ElapsedMilliseconds}ms");
			});

			// Run through all of the pack textures
			Task packTask = Task.Run(() =>
			{
				Stopwatch st = Stopwatch.StartNew();
				if (UseParallel)
				{
					Parallel.ForEach(packFiles, (x) =>
					{
						if (!blacklist.Any(rule => Regex.IsMatch(x, rule)) && !refFiles.Contains(x))
							UnusedTexturesList.Add(x); // MC doesn't contain this texture
					});
				}
				else
				{
					packFiles.ForEach(x =>
					{
						if (!blacklist.Any(rule => Regex.IsMatch(x, rule)) && !refFiles.Contains(x))
							UnusedTexturesList.Add(x); // MC doesn't contain this texture
					});
				}
				st.Stop();

				if (PerfLogging)
					Console.WriteLine($"Ran through all pack textures in {st.ElapsedMilliseconds}ms");
			});
			await Task.WhenAll(refTask, packTask); // Run async to improve speed
		}

		/// <summary>
		/// Get a list of all textures within the uploaded resource pack
		/// </summary>
		/// <returns>List of textures</returns>
		private async Task<List<string>> GetPackFileList()
		{
			await using MemoryStream ms = new MemoryStream(MAX_FILESIZE_BYTES);
			await File.OpenReadStream(MAX_FILESIZE_BYTES).CopyToAsync(ms);

			List<string> usefulFiles = new List<string>();
			using (ZipArchive zip = new ZipArchive(ms))
			{
				Stopwatch st = Stopwatch.StartNew();
				if (UseParallel)
				{
					Parallel.ForEach(zip.Entries, (file) =>
					{
						// Include all PNGs, if Bedrock, include TGAs
						if (file.FullName.EndsWith("png") || (SelectedEdition == MCEdition.Bedrock && file.FullName.EndsWith("tga")))
							usefulFiles.Add(file.FullName);
					});
				}
				else
				{
					foreach (ZipArchiveEntry file in zip.Entries)
					{
						// Include all PNGs, if Bedrock, include TGAs
						if (file.FullName.EndsWith("png") || (SelectedEdition == MCEdition.Bedrock && file.FullName.EndsWith("tga")))
							usefulFiles.Add(file.FullName);
					}
				}
				st.Stop();

				if (PerfLogging)
					Console.WriteLine($"Got all pack textures in {st.ElapsedMilliseconds}ms");
			}
			return usefulFiles;
		}
		#endregion

		#region Blacklist
		/// <summary>
		/// Open Blacklist Dialog with the appropriate regex list
		/// </summary>
		private void OpenBlacklistDialog()
		{
			DialogOptions options = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true };
			#pragma warning disable CS8974 // Converting method group to non-delegate type
			DialogParameters parameters = new DialogParameters()
			{
				{"Edition", SelectedEdition},
				{"Blacklist", SelectedEdition == MCEdition.Java ? BlacklistRegexJava : BlacklistRegexBedrock},
				{"DefaultBlacklist", SelectedEdition == MCEdition.Java ? DefaultBlacklistJava : DefaultBlacklistBedrock},
				{"Callback", UpdateBlacklist } // Callback to update blacklist
			};
			#pragma warning restore CS8974 // Converting method group to non-delegate type
			Dialog.Show<TexturesBlacklistDialog>("Custom Blacklist", parameters, options);
		}

		/// <summary>
		/// Update blacklist for the specified edition
		/// </summary>
		/// <param name="edition">Blacklist edition to update (Java/Bedrock)</param>
		/// <param name="blacklist">New blacklist</param>
		private void UpdateBlacklist(MCEdition edition, List<string> blacklist)
		{
			Console.WriteLine($"Updated: {string.Join(", ", blacklist)}");
			if (edition == MCEdition.Java)
				BlacklistRegexJava = blacklist;
			else
				BlacklistRegexBedrock = blacklist;
		}

		/// <summary>
		/// Reset both blacklists
		/// </summary>
		private async Task ResetBothBlacklists()
		{
			await Task.WhenAll(ResetBlacklist(MCEdition.Java), ResetBlacklist(MCEdition.Bedrock));
		}
		#endregion

		#region Upload
		/// <summary>
		/// Upload resource pack
		/// </summary>
		/// <param name="e"></param>
		private void UploadFile(InputFileChangeEventArgs e)
		{
			List<string> errors = PackValidation(e.File);
			if (errors.Count > 0) // Show warnings for any validation errors
			{
				errors.ForEach(x => Snackbar.Add(x, Severity.Warning));
				File = null;
			}
			else // Pack is valid
			{
				Snackbar.Add("Resource pack uploaded!", Severity.Success);
				File = e.File;
			}

			StateHasChanged();
		}

		/// <summary>
		/// Ensure the uploaded file is valid
		/// </summary>
		/// <param name="file">Uploaded file</param>
		/// <returns>A list of errors</returns>
		private List<string> PackValidation(IBrowserFile file)
		{
			List<string> errors = new List<string>();
			if (file.Size > MAX_FILESIZE_BYTES) // Limit max filesize for resource pack
				errors.Add($"Uploads cannot be greater than {MAX_FILESIZE_MB}MB");

			string fileType = file.Name.Split('.').Last();
			if (SelectedEdition == MCEdition.Java)
			{
				if (fileType != "zip") // Only support zip files for Java
					errors.Add($"Only zip files are supported");
			}
			else
			{
				if (fileType is not ("zip" or "mcpack")) // Only support zip & mcpack files for Bedrock
					errors.Add($"Only zip and mcpack files are supported");
			}
			return errors;
		}

		private string GetSupportedFileTypes => SelectedEdition == MCEdition.Java ? ".zip" : ".zip, .mcpack";
		#endregion

		#region Export
		/// <summary>
		/// Export a text file containing the matching textures
		/// </summary>
		private async Task ExportMatchingTextures()
		{
			await Export(MatchingTexturesList, $"MatchingTextures_{File.Name}.txt");
		}

		/// <summary>
		/// Export a text file containing the missing textures
		/// </summary>
		private async Task ExportMissingTextures()
		{
			await Export(MissingTexturesList, $"MissingTextures_{File.Name}.txt");
		}

		/// <summary>
		/// Export a text file containing the unused textures
		/// </summary>
		private async Task ExportUnusedTextures()
		{
			await Export(UnusedTexturesList, $"UnusedTextures_{File.Name}.txt");
		}

		/// <summary>
		/// Copy String List to clipboard
		/// </summary>
		/// <param name="list">List of strings</param>
		private async Task CopyTextToClipboard(List<string> list)
		{
			await jsHelper.CopyTextToClipboard(list);
		}

		/// <summary>
		/// Export a text file containing a list of strings
		/// </summary>
		/// <param name="listToExport">List of string to export (each on a new line)</param>
		/// <param name="fileName">Exported files filename</param>
		private async Task Export(List<string> listToExport, string fileName)
		{
			await jsHelper.ExportListToFile(listToExport, fileName);
		}
		#endregion
	}
}
