using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MCTools.Enums;
using MCTools.Models;
using MCTools.Shared.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
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
        private const bool DisableBedrockSupport = true;
        #endregion

        #region API Values
        private List<MCVersion> MinecraftVersions { get; set; } = new List<MCVersion>();
        private MCVersion LatestVersion { get; set; }
        private MCAssets Assets { get; set; }
        #endregion

        #region Options
        private string UploadText => "Upload Resource Pack" + (File != null ? $": {File.Name}" : "");
        private MCVersion SelectedVersion { get; set; }
        private MCEdition SelectedEdition { get; set; } = MCEdition.Java;

        private bool CanCompare => File is { Size: > 0 } && SelectedVersion != null && SelectedEdition > 0;

        private bool ExcludeRealms { get; set; } = true;
        private bool ExcludeFonts { get; set; } = true;
        private bool ExcludeOptifine { get; set; } = true;
        private bool ExcludeBedrockUI { get; set; }

        private List<string> BlacklistRegexJava = new List<string>();
        private List<string> BlacklistRegexBedrock = new List<string>();

        #region Defaults
        private readonly List<string> DefaultBlacklistJava = new List<string>()
            { @"assets\/minecraft\/textures\/ctm", @"assets\/minecraft\/textures\/custom", @"textures\/colormap", @"background\/panorama_overlay.png" };

        private readonly List<string> DefaultBlacklistBedrock = new List<string>()
            { @"texts\/", @"textures\/persona_thumbnails" };
        #endregion
        #endregion

        #region Results
        public IBrowserFile File { get; set; }

        private List<string> MatchingTexturesList = new List<string>();
        private List<string> MissingTexturesList = new List<string>();
        private List<string> UnusedTexturesList = new List<string>();

        private int TotalTextures = 0;
        #endregion

        #endregion

        #region Blazor Overrides
        protected override async Task OnInitializedAsync()
        {
            try
            {
                MinecraftVersions = await _apiController.GetVersions();
                if (MinecraftVersions is { Count: > 0 })
                {
                    LatestVersion = MinecraftVersions.First(x => x.Type == "release");
                    SelectedVersion = LatestVersion;
                }
                else
                    Snackbar.Add("Unable to fetch versions! Is the API down?", Severity.Error);
            }
            catch (Exception)
            {
                Snackbar.Add("An error occurred when loading versions! Check the console for errors.", Severity.Error);
                throw;
            }

            await SetBlacklistFromLocalStorage();
            StateHasChanged();
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
                await localStore.SetItemAsStringAsync("blacklistBedrock", JsonConvert.SerializeObject(BlacklistRegexJava));
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
            List<string> tempBlackList = new List<string>();

            if (SelectedEdition == MCEdition.Java)
            {
                tempBlackList.AddRange(BlacklistRegexJava);

                if (ExcludeRealms)
                    tempBlackList.Add(@"assets\/realms");
                if (ExcludeFonts)
                    tempBlackList.Add(@"textures\/font");
                if (ExcludeOptifine)
                    tempBlackList.Add(@"assets\/minecraft\/optifine");
            }
            else
            {
                tempBlackList.AddRange(BlacklistRegexBedrock);

                if (ExcludeFonts)
                    tempBlackList.Add(@"font\/");
                if (ExcludeBedrockUI)
                {
                    tempBlackList.Add(@"textures\/gui");
                    tempBlackList.Add(@"textures\/ui");
                }
            }

            try
            {
                Assets = await _apiController.GetAssets(SelectedVersion.Id);
                if (Assets.Textures is not { Count: > 0 })
                {
                    Snackbar.Add("Unable to load assets! Is the API down?", Severity.Error);
                    return;
                }
            }
            catch (Exception)
            {
                Snackbar.Add("An error occurred when loading assets! Check the console for errors.", Severity.Error);
                throw;
            }
            await CompareTextures((await GetPackFileList() ?? new List<string>()), Assets.Textures, tempBlackList);
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
            });

            // Run through all of the pack textures
            Task packTask = Task.Run(() =>
            {
                packFiles.ForEach(x =>
                {
                    if (!blacklist.Any(rule => Regex.IsMatch(x, rule)) && !refFiles.Contains(x))
                        UnusedTexturesList.Add(x); // MC doesn't contain this texture
                });
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
                foreach (ZipArchiveEntry file in zip.Entries)
                {
                    // Include all PNGs, if Bedrock, include TGAs
                    if (file.FullName.EndsWith("png") || (SelectedEdition == MCEdition.Bedrock && file.FullName.EndsWith("tga")))
                        usefulFiles.Add(file.FullName);
                }
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

            if (SelectedEdition == MCEdition.Java)
            {
                if (file.Name.Split('.').Last() != "zip") // Only support zip files for Java
                    errors.Add($"Only zip files are supported");
            }
            else
            {
                if (file.Name.Split('.').Last() != "zip" || file.Name.Split('.').Last() != "mcpack") // Only support zip & mcpack files for Bedrock
                    errors.Add($"Only zip and mcpack files are supported");
            }
            return errors;
        }
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
        /// Export a text file containing a list of strings
        /// </summary>
        /// <param name="listToExport">List of string to export (each on a new line)</param>
        /// <param name="fileName">Exported files filename</param>
        private async Task Export(List<string> listToExport, string fileName)
        {
            using var streamRef = new DotNetStreamReference(new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", listToExport))));
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
        #endregion
    }
}
