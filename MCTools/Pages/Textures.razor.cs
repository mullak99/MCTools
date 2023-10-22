using MCTools.Enums;
using MCTools.Logic;
using MCTools.Models;
using MCTools.SDK.Models;
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

		#region API Values
		private MCAssets Assets { get; set; }
		#endregion

		#region Options
		private string UploadText => "Upload Resource Pack" + (Pack != null ? $": {Pack.Name}" : "");
		private MCVersion SelectedVersion;
		private MCEdition SelectedEdition;
		private bool IsOverlaySupported;
		private bool IsProcessing;

		private bool CanCompare => Pack is { Size: > 0 } && SelectedVersion != null && SelectedEdition > 0;
		private bool CanShowMcMetasSections => !ExcludeMcMetas && DidCompareMcMetas && SelectedEdition == MCEdition.Java;

		private bool DidCompareMcMetas;

		private bool ExcludeRealms { get; set; } = true;
		private bool ExcludeFonts { get; set; } = true;
		private bool ExcludeOptifine { get; set; } = true;
		private bool ExcludeMisc { get; set; } = true;
		private bool ExcludeNonVanillaNamespaces { get; set; } = true;
		private bool ExcludeEmissives { get; set; } = true;
		private bool ExcludeTitleGui { get; set; } = true;
		private bool ExcludeMcMetas { get; set; }

		private bool ExcludeBedrockUI { get; set; }

		private bool PerfLogging { get; set; }
		private bool ResourcePackDebug { get; set; }

		private List<string> BlacklistRegexJava = new();
		private List<string> BlacklistRegexBedrock = new();

		private async Task SelectedVersionChanged(MCVersion version)
		{
			SelectedVersion = version;
			IsOverlaySupported = await GetOverlaySupport();
		}

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
				@"textures\/colormap",
				@"textures\/trims\/color_palettes",
				@"textures\/environment\/clouds.png",
				@"textures\/environment\/end_portal_colors.png",
				@"textures\/entity\/horse\/armor\/horse_armor_none.png",
				@"textures\/entity\/horse\/horse_markings_none.png",
				@"textures\/entity\/horse2\/armor\/horse_armor_none.png",
				@"textures\/entity\/horse2\/horse_markings_none.png",
				@"textures\/entity\/iron_golem\/cracked_none.png",
				@"textures\/entity\/llama\/decor\/decor_none.png",
				@"textures\/entity\/llama\/spit.png",
				@"textures\/entity\/lead_rope.png",
				@"textures\/entity\/loyalty_rope.png",
				@"textures\/entity\/cape_invisible.png",
				@"textures\/entity\/zombie_villager2\/professions\/unskilled.tga"
			};
		#endregion
		#endregion

		#region Results
		public ResourcePack Pack { get; set; }

		private List<string> MatchingTexturesList = new();
		private List<string> MissingTexturesList = new();
		private List<string> UnusedTexturesList = new();

		private List<string> MatchingMcMetasList = new();
		private List<string> MissingMcMetasList = new();
		private List<string> UnusedMcMetasList = new();

		private int TotalTextures;
		private int TotalMcMetas;
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
			string rawJavaBL = await LocalStore.GetItemAsStringAsync("blacklistJava");
			string rawBedrockBL = await LocalStore.GetItemAsStringAsync("blacklistBedrock");

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
				await LocalStore.SetItemAsStringAsync("blacklistJava", JsonConvert.SerializeObject(BlacklistRegexJava));
			}
			else
			{
				BlacklistRegexBedrock = DefaultBlacklistBedrock;
				await LocalStore.SetItemAsStringAsync("blacklistBedrock", JsonConvert.SerializeObject(BlacklistRegexBedrock));
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
					? await JavaController.GetAssets(SelectedVersion.Id)
					: await BedrockController.GetAssets(SelectedVersion.Id);

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
			await Compare(Assets.Textures, Assets.McMetas, tempBlackList);
			IsProcessing = false;
			StateHasChanged();
		}

		/// <summary>
		/// Reset Textures Tool UI
		/// </summary>
		private void Reset()
		{
			MatchingTexturesList = new();
			MissingTexturesList = new();
			UnusedTexturesList = new();

			MatchingMcMetasList = new();
			MissingMcMetasList = new();
			UnusedMcMetasList = new();

			DidCompareMcMetas = false;

			Pack = null;
			TotalTextures = 0;
			TotalMcMetas = 0;
		}
		#endregion

		/// <summary>
		/// Compare uploaded pack against Minecraft's assets
		/// </summary>
		/// <param name="refTexFiles">List of Minecraft official texture assets</param>
		/// <param name="refMcMetaFiles">List of Minecraft official mcmeta assets</param>
		/// <param name="blacklist">Regex statements for assets to ignore</param>
		/// <returns></returns>
		private async Task Compare(List<string> refTexFiles, List<string> refMcMetaFiles, List<string> blacklist)
		{
			Stopwatch st = Stopwatch.StartNew();
			bool success = await Pack.Process(Validation.GetMaxFileSizeBytes());
			st.Stop();

			if (!success)
			{
				Snackbar.Add("Unable to process resource pack!", Severity.Error);
				return;
			}

			if (PerfLogging)
				Console.WriteLine($"Processed pack in {st.ElapsedMilliseconds}ms");

			List<Task> tasks = new()
			{
				CompareTextures(refTexFiles, blacklist)
			};

			if (!ExcludeMcMetas)
			{
				DidCompareMcMetas = true;
				tasks.Add(CompareMcMetas(refMcMetaFiles, blacklist));
			}
			else DidCompareMcMetas = false;

			await Task.WhenAll(tasks);
		}

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

		/// <summary>
		/// Compare uploaded packs mcmetas against Minecraft's mcmetas list
		/// </summary>
		/// <param name="refFiles">List of Minecraft official mcmetas assets</param>
		/// <param name="blacklist">Regex statements for mcmetas to ignore</param>
		private async Task CompareMcMetas(List<string> refFiles, List<string> blacklist)
		{
			// Clear texture lists
			MatchingMcMetasList.Clear();
			MissingMcMetasList.Clear();
			UnusedMcMetasList.Clear();
			TotalMcMetas = 0;

			List<string> packFiles = Pack.GetMcMetas();

			// Run through all of the reference (MC) MCMetas
			Task refTask = Task.Run(() =>
			{
				Stopwatch rSt = Stopwatch.StartNew();
				refFiles.ForEach(x =>
				{
					if (!blacklist.Any(rule => Regex.IsMatch(x, rule)))
					{
						TotalMcMetas++;
						if (packFiles.Contains(x))
							MatchingMcMetasList.Add(x); // Pack contains this mcmeta
						else
							MissingMcMetasList.Add(x); // Pack doesn't contain this mcmeta
					}
				});
				rSt.Stop();

				if (PerfLogging)
					Console.WriteLine($"Ran through all reference MCMetas in {rSt.ElapsedMilliseconds}ms");
			});

			// Run through all of the pack MCMetas
			Task packTask = Task.Run(() =>
			{
				Stopwatch pSt = Stopwatch.StartNew();
				packFiles.ForEach(x =>
				{
					if (!blacklist.Any(rule => Regex.IsMatch(x, rule)) && !refFiles.Contains(x))
						UnusedMcMetasList.Add(x); // MC doesn't contain this mcmeta
				});
				pSt.Stop();

				if (PerfLogging)
					Console.WriteLine($"Ran through all pack MCMetas in {pSt.ElapsedMilliseconds}ms");
			});

			Stopwatch tSt = Stopwatch.StartNew();
			await Task.WhenAll(refTask, packTask); // Run async to improve speed
			tSt.Stop();
			if (PerfLogging)
				Console.WriteLine($"Compared MCMetas in {tSt.ElapsedMilliseconds}ms");
		}

		private async Task<bool> GetOverlaySupport()
			=> SelectedEdition == MCEdition.Java && await JavaController.GetOverlaySupport(SelectedVersion.Id);
		#endregion

		#region Blacklist
		/// <summary>
		/// Open Blacklist Dialog with the appropriate regex list
		/// </summary>
		private void OpenBlacklistDialog(MCEdition edition)
		{
			DialogOptions options = new() { MaxWidth = MaxWidth.Medium, FullWidth = true };
			#pragma warning disable CS8974 // Converting method group to non-delegate type
			DialogParameters parameters = new()
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
			DialogOptions options = new() { MaxWidth = MaxWidth.Small, FullWidth = true };
			#pragma warning disable CS8974 // Converting method group to non-delegate type
			DialogParameters parameters = new()
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
			IsProcessing = false;
			StateHasChanged();
		}
		#endregion

		#region Export
		#region Textures
		/// <summary>
		/// Export a text file containing the matching textures
		/// </summary>
		private async Task ExportMatchingTextures()
			=> await InputOutputUtils.Export(JsHelper, MatchingTexturesList, $"MatchingTextures_{Pack.Name}.txt");

		/// <summary>
		/// Export a text file containing the missing textures
		/// </summary>
		private async Task ExportMissingTextures()
			=> await InputOutputUtils.Export(JsHelper, MissingTexturesList, $"MissingTextures_{Pack.Name}.txt");

		/// <summary>
		/// Export a text file containing the unused textures
		/// </summary>
		private async Task ExportUnusedTextures()
			=> await InputOutputUtils.Export(JsHelper, UnusedTexturesList, $"UnusedTextures_{Pack.Name}.txt");
		#endregion

		#region MCMetas
		/// <summary>
		/// Export a text file containing the matching mcmetas
		/// </summary>
		private async Task ExportMatchingMcMetas()
			=> await InputOutputUtils.Export(JsHelper, MatchingMcMetasList, $"MatchingMCMetas_{Pack.Name}.txt");

		/// <summary>
		/// Export a text file containing the missing mcmetas
		/// </summary>
		private async Task ExportMissingMcMetas()
			=> await InputOutputUtils.Export(JsHelper, MissingMcMetasList, $"MissingMCMetas_{Pack.Name}.txt");

		/// <summary>
		/// Export a text file containing the unused mcmetas
		/// </summary>
		private async Task ExportUnusedMcMetas()
			=> await InputOutputUtils.Export(JsHelper, UnusedMcMetasList, $"UnusedMCMetas_{Pack.Name}.txt");
		#endregion
		#endregion
	}
}
