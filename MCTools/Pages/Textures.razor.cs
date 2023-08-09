using MCTools.Enums;
using MCTools.Models;
using MCTools.Shared.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
		private MCAssets Assets { get; set; }
		#endregion

		#region Options
		private string UploadText => "Upload Resource Pack" + (Pack != null ? $": {Pack.Name}" : "");
		private MCVersion SelectedVersion;
		private MCEdition SelectedEdition;
		private bool IsProcessing;

		private bool CanCompare => Pack is { Size: > 0 } && SelectedVersion != null && SelectedEdition > 0;

		private bool ExcludeRealms { get; set; } = true;
		private bool ExcludeFonts { get; set; } = true;
		private bool ExcludeOptifine { get; set; } = true;
		private bool ExcludeMisc { get; set; } = true;
		private bool ExcludeNonVanillaNamespaces { get; set; } = true;
		private bool ExcludeEmissives { get; set; } = true;
		private bool ExcludeTitleGui { get; set; } = true;

		private bool ExcludeBedrockUI { get; set; }

		private bool PerfLogging { get; set; }

		private List<string> BlacklistRegexJava = new();
		private List<string> BlacklistRegexBedrock = new();

		private void SelectedVersionChanged(MCVersion version)
			=> SelectedVersion = version;

		private void SelectedEditionChanged(MCEdition edition)
			=> SelectedEdition = edition;

		#region Defaults
		private readonly List<string> DefaultBlacklistJava = new()
			{
				@"_MACOSX",
				@"assets\/minecraft\/textures\/ctm",
				@"assets\/minecraft\/textures\/custom",
				@"textures\/colormap",
				@"background\/panorama_overlay.png",
				@"assets\/minecraft\/textures\/environment\/clouds.png",
				@"assets\/minecraft\/textures\/trims\/color_palettes",
				@"assets\/minecraft\/textures\/gui\/presets",
				@"assets\/minecraft\/textures\/entity\/llama\/spit.png",
				@"assets\/minecraft\/textures\/block\/lightning_rod_on.png"
			};

		private readonly List<string> DefaultBlacklistBedrock = new()
			{
				@"_MACOSX",
				@"texts\/",
				@"textures\/persona_thumbnails",
				@"textures\/colormap"
			};
		#endregion
		#endregion

		#region Results
		public ResourcePack Pack { get; set; }

		private List<string> MatchingTexturesList = new();
		private List<string> MissingTexturesList = new();
		private List<string> UnusedTexturesList = new();

		private int TotalTextures;
		#endregion
		#endregion

		#region Blazor Overrides
		protected override async Task OnInitializedAsync()
		{
			await SetBlacklistFromLocalStorage();
		}
		#endregion

		#region Local Storage
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
				await localStore.SetItemAsStringAsync("blacklistBedrock", JsonConvert.SerializeObject(BlacklistRegexBedrock));
			}
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
			List<string> tempBlackList = new();

			if (SelectedEdition == MCEdition.Java)
			{
				tempBlackList.AddRange(BlacklistRegexJava);

				if (ExcludeRealms)
					tempBlackList.AddRange(new List<string>
					{
						@"assets\/realms", @"assets\/minecraft\/textures\/gui\/realms"
					});
				if (ExcludeFonts)
					tempBlackList.Add(@"textures\/font");
				if (ExcludeMisc)
					tempBlackList.Add(@"textures\/misc");
				if (ExcludeOptifine)
					tempBlackList.Add(@"assets\/minecraft\/optifine");
				if (ExcludeNonVanillaNamespaces)
					tempBlackList.Add(@"assets\/\b(?!minecraft\b|realms\b).*?\b");
				if (ExcludeEmissives)
					tempBlackList.Add(@"textures\/.+\/.+_e(missive)?\.png");
				if (ExcludeTitleGui)
					tempBlackList.Add(@"assets\/minecraft\/textures\/gui\/title");
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
			await CompareTextures(Assets.Textures, tempBlackList);
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
			Pack = null;
			TotalTextures = 0;
		}
		#endregion

		/// <summary>
		/// Compare uploaded packs textures against Minecraft's texture list
		/// </summary>
		/// <param name="refFiles">List of Minecraft official texture assets</param>
		/// <param name="blacklist">Regex statements for textures to ignore</param>
		/// <returns></returns>
		private async Task CompareTextures(List<string> refFiles, List<string> blacklist)
		{
			// Clear texture lists
			MatchingTexturesList.Clear();
			MissingTexturesList.Clear();
			UnusedTexturesList.Clear();
			TotalTextures = 0;

			Stopwatch st = Stopwatch.StartNew();
			await Pack.Process(MAX_FILESIZE_BYTES);
			st.Stop();

			if (PerfLogging)
				Console.WriteLine($"Got all pack textures in {st.ElapsedMilliseconds}ms");

			List<string> packFiles = Pack.GetTextures();

			// Run through all of the reference (MC) textures
			Task refTask = Task.Run(() =>
			{
				Stopwatch rSt = Stopwatch.StartNew();
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
				rSt.Stop();

				if (PerfLogging)
					Console.WriteLine($"Ran through all reference textures in {rSt.ElapsedMilliseconds}ms");
			});

			// Run through all of the pack textures
			Task packTask = Task.Run(() =>
			{
				Stopwatch pSt = Stopwatch.StartNew();
				packFiles.ForEach(x =>
				{
					if (!blacklist.Any(rule => Regex.IsMatch(x, rule)) && !refFiles.Contains(x))
						UnusedTexturesList.Add(x); // MC doesn't contain this texture
				});
				pSt.Stop();

				if (PerfLogging)
					Console.WriteLine($"Ran through all pack textures in {pSt.ElapsedMilliseconds}ms");
			});

			Stopwatch tSt = Stopwatch.StartNew();
			await Task.WhenAll(refTask, packTask); // Run async to improve speed
			tSt.Stop();
			if (PerfLogging)
				Console.WriteLine($"Compared textures in {tSt.ElapsedMilliseconds}ms");
		}
		#endregion

		#region Blacklist
		/// <summary>
		/// Open Blacklist Dialog with the appropriate regex list
		/// </summary>
		private void OpenBlacklistDialog(MCEdition edition)
		{
			DialogOptions options = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true };
			#pragma warning disable CS8974 // Converting method group to non-delegate type
			DialogParameters parameters = new DialogParameters()
			{
				{"Edition", edition},
				{"Blacklist", edition == MCEdition.Java ? BlacklistRegexJava : BlacklistRegexBedrock},
				{"DefaultBlacklist", edition == MCEdition.Java ? DefaultBlacklistJava : DefaultBlacklistBedrock},
				{"Callback", UpdateBlacklist } // Callback to update blacklist
			};
			#pragma warning restore CS8974 // Converting method group to non-delegate type

			string title = $"Custom Blacklist ({(edition == MCEdition.Java ? "Java" : "Bedrock")})";
			Dialog.Show<TexturesBlacklistDialog>(title, parameters, options);
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
		/// Open Reset Blacklist Confirmation Dialog
		/// </summary>
		private void OpenResetConfirmationDialog()
		{
			DialogOptions options = new DialogOptions() { MaxWidth = MaxWidth.Small, FullWidth = true };
			#pragma warning disable CS8974 // Converting method group to non-delegate type
			DialogParameters parameters = new DialogParameters()
			{
				{"ConfirmationText", "Are you sure you want to reset both blacklists?"},
				{"Callback", ResetBlacklistCallback}
			};
			#pragma warning restore CS8974 // Converting method group to non-delegate type
			Dialog.Show<ConfirmationDialog>("Reset Both Blacklists?", parameters, options);
		}

		/// <summary>
		/// Callback to handle blacklist resetting
		/// </summary>
		/// <param name="reset">Whether to reset the blacklists</param>
		private void ResetBlacklistCallback(bool reset)
		{
			if (reset)
				ResetBothBlacklists().ConfigureAwait(true);
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
		private async Task UploadFile(InputFileChangeEventArgs e)
		{
			IsProcessing = true;
			List<string> errors = PackValidation(e.File);
			if (errors.Count > 0) // Show warnings for any validation errors
			{
				errors.ForEach(x => Snackbar.Add(x, Severity.Warning));
				Pack = null;
			}
			else // Pack is valid
			{
				Snackbar.Add("Resource pack uploaded!", Severity.Success);
				Pack = new(e.File, SelectedEdition);
				await Pack.Init(MAX_FILESIZE_BYTES);
			}
			IsProcessing = false;
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
					errors.Add("Only zip files are supported");
			}
			else
			{
				if (fileType is not ("zip" or "mcpack")) // Only support zip & mcpack files for Bedrock
					errors.Add("Only zip and mcpack files are supported");
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
			=> await Export(MatchingTexturesList, $"MatchingTextures_{Pack.Name}.txt");

		/// <summary>
		/// Export a text file containing the missing textures
		/// </summary>
		private async Task ExportMissingTextures()
			=> await Export(MissingTexturesList, $"MissingTextures_{Pack.Name}.txt");

		/// <summary>
		/// Export a text file containing the unused textures
		/// </summary>
		private async Task ExportUnusedTextures()
			=> await Export(UnusedTexturesList, $"UnusedTextures_{Pack.Name}.txt");

		/// <summary>
		/// Copy String List to clipboard
		/// </summary>
		/// <param name="list">List of strings</param>
		private async Task CopyTextToClipboard(List<string> list)
			=> await _JsHelper.CopyTextToClipboard(list);

		/// <summary>
		/// Export a text file containing a list of strings
		/// </summary>
		/// <param name="listToExport">List of string to export (each on a new line)</param>
		/// <param name="fileName">Exported files filename</param>
		private async Task Export(List<string> listToExport, string fileName)
			=> await _JsHelper.ExportListToFile(listToExport, fileName);
		#endregion
	}
}
