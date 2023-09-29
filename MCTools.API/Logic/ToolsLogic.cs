﻿using System.IO.Compression;
using System.Text.RegularExpressions;
using MCTools.API.Extentions;
using MCTools.API.Repository;
using MCTools.SDK.Enums;
using MCTools.SDK.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Octokit;
using FileMode = System.IO.FileMode;

namespace MCTools.API.Logic
{
	public class ToolsLogic : IToolsLogic
	{
		private const int ASSET_VERSION = 2;
		private const string BEDROCK_ZIP_REGEX = "Mojang-bedrock-samples-[a-zA-Z0-9]+\\/resource_pack";

		private readonly IVersionAssetsRepository _vaRepository;
		private readonly IGitHubClient _client;
		private readonly ILogger<ToolsLogic> _logger;
		private readonly HttpClient _httpClient;

		public ToolsLogic(IVersionAssetsRepository vaRepository, IGitHubClient githubClient, ILogger<ToolsLogic> logger, HttpClient httpClient)
		{
			_vaRepository = vaRepository;
			_client = githubClient;
			_logger = logger;
			_httpClient = httpClient;
		}

		private readonly DefaultContractResolver contractResolver = new()
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		};

		public async Task<ResponseModel<MCAssets>> GetMinecraftJavaAssets(string version, bool bypassHighestVersionLimit = false)
		{
			List<AssetMCVersion> supportedVersions = await GetJavaMCVersions(bypassHighestVersionLimit);

			return await GetMinecraftAssets(version, MinecraftEdition.Java, supportedVersions);
		}

		public async Task<ResponseModel<MCAssets>> GetMinecraftBedrockAssets(string version)
		{
			List<AssetMCVersion> supportedVersions = await GetBedrockMCVersions();

			return await GetMinecraftAssets(version, MinecraftEdition.Bedrock, supportedVersions);
		}

		public async Task<ResponseModel<string>> GetMinecraftJavaJar(string version)
		{
			List<AssetMCVersion> supportedVersions = await GetJavaMCVersions();
			AssetMCVersion? mcVer = supportedVersions.FirstOrDefault(x => x.Id == version && x.Edition == "java");

			string jUrl = string.Empty;
			if (mcVer != null)
				jUrl = await GetJarDownload(mcVer);

			if (string.IsNullOrWhiteSpace(jUrl))
				return new()
				{
					IsSuccess = false,
					Message = "Invalid version"
				};

			return new()
			{
				IsSuccess = true,
				Data = jUrl
			};
		}

		public async Task PurgeAssets()
		{
			List<MinecraftVersionAssets> allAssets = await _vaRepository.GetAllVersionAssets();

			Task<List<AssetMCVersion>> javaVersionsTask = GetJavaMCVersions(true);
			Task<List<AssetMCVersion>> bedrockVersionsTask = GetBedrockMCVersions();

			await Task.WhenAll(javaVersionsTask, bedrockVersionsTask);

			List<AssetMCVersion> allVersions = javaVersionsTask.Result.Concat(bedrockVersionsTask.Result).ToList();
			List<MinecraftVersionAssets> unsupportedAssets = allAssets
				.Where(x => !allVersions.Select(y => y.Id).Contains(x.Name) || x.Version != ASSET_VERSION).ToList();

			List<Task> removeTasks = unsupportedAssets.Select(asset => _vaRepository.DeleteVersionAssets(asset.Name, asset.Edition, asset.Version)).Cast<Task>().ToList();
			await removeTasks.WhenAllThrottledAsync(25);
		}

		public async Task<bool> PregenerateJavaAssets(List<AssetMCVersion>? versions = null, bool bypassHighestVersionLimit = false)
		{
			if (versions == null || versions.Count == 0)
				versions = await GetJavaMCVersions(bypassHighestVersionLimit);

			List<Task> tasks = versions.Select(ver => GetMinecraftAssets(ver.Id, MinecraftEdition.Java, versions)).Cast<Task>().ToList();
			await Task.WhenAll(tasks);

			return true;
		}

		public async Task<bool> PregenerateBedrockAssets(List<AssetMCVersion>? versions = null)
		{
			if (versions == null || versions.Count == 0)
				versions = await GetBedrockMCVersions();

			List<Task> tasks = versions.Select(ver => GetMinecraftAssets(ver.Id, MinecraftEdition.Bedrock, versions)).Cast<Task>().ToList();
			await Task.WhenAll(tasks);

			return true;
		}

		public async Task<List<AssetMCVersion>> GetJavaMCVersions(bool bypassHighestVersionLimit = false)
		{
			try
			{
				using var httpResponse = await _httpClient.GetAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json", HttpCompletionOption.ResponseHeadersRead);
				httpResponse.EnsureSuccessStatusCode();

				var rawJson = await httpResponse.Content.ReadAsStringAsync();
				var json = JObject.Parse(rawJson);

				JToken? latestReleaseT = json.SelectToken("$.latest.release");
				JToken? latestSnapshotT = json.SelectToken("$.latest.snapshot");

				string latestRelease = latestReleaseT != null ? latestReleaseT.ToString() : "";
				string latestSnapshot = latestSnapshotT != null ? latestSnapshotT.ToString() : "";

				List<AssetMCVersion> versions = new();
				json.SelectTokens($"$.versions[?(@.type == 'release' || @.id == '{latestSnapshot}' || @.id == '{latestRelease}')]")
					.ToList().ForEach(x =>
					{
						var obj = x.ToObject<AssetMCVersion>();
						if (obj != null)
						{
							obj.Edition = "java";
							versions.Add(obj);
						}
					});
				return LimitVersions(versions, bypassHighestVersionLimit);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Failed to get Java MC versions");
				throw;
			}
		}

		public async Task<List<AssetMCVersion>> GetBedrockMCVersions()
		{
			try
			{
				List<AssetMCVersion> versions = new();
				var allReleases = await _client.Repository.Release.GetAll("Mojang", "bedrock-samples");

				Release? preRelease = allReleases.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.Prerelease);
				if (preRelease != null)
				{
					versions.Add(new()
					{
						Id = TrimBedrockReleaseId(preRelease.Name),
						ReleaseTime = preRelease.CreatedAt.UtcDateTime,
						Time = preRelease.CreatedAt.UtcDateTime,
						Type = "beta",
						Edition = "bedrock",
						Url = preRelease.ZipballUrl
					});
				}

				Release? release = allReleases.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.Prerelease == false);
				if (release != null)
				{
					versions.Add(new()
					{
						Id = TrimBedrockReleaseId(release.Name),
						ReleaseTime = release.CreatedAt.UtcDateTime,
						Time = preRelease?.CreatedAt.UtcDateTime ?? DateTime.UtcNow,
						Type = "release",
						Edition = "bedrock",
						Url = release.ZipballUrl
					});
				}
				return versions;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Failed to get Bedrock MC versions");
				throw;
			}
		}

		private async Task<ResponseModel<MCAssets>> GetMinecraftAssets(string version, MinecraftEdition minecraftEdition, List<AssetMCVersion> supportedVersions)
		{
			string editionString = minecraftEdition == MinecraftEdition.Java ? "java" : "bedrock";
			AssetMCVersion? mcVer = supportedVersions.FirstOrDefault(x => x.Id == version && x.Edition == editionString);

			// Version is technically unsupported, but it still might exist in the db.
			// This is basically a client-side cache work-around (old entries will eventually be wiped from the db).
			if (mcVer == null)
			{
				MinecraftVersionAssets? existingAsset = await _vaRepository.GetVersionAssets(version, editionString, ASSET_VERSION);
				if (existingAsset != null)
				{
					MCAssets? data = JsonConvert.DeserializeObject<MCAssets>(existingAsset.JSON);
					if (data != null)
					{
						return new()
						{
							IsSuccess = true,
							Data = data
						};
					}
				}
			}
			else
			{
				MinecraftVersionAssets? existingAsset = await _vaRepository.GetVersionAssets(version, editionString, ASSET_VERSION);

				if (existingAsset == null)
				{
					return new()
					{
						IsSuccess = true,
						Data = await CreateMCVA(mcVer, minecraftEdition)
					};
				}
				MCAssets? data = JsonConvert.DeserializeObject<MCAssets>(existingAsset.JSON);
				if (data != null)
				{
					return new()
					{
						IsSuccess = true,
						Data = data
					};
				}
			}
			return new()
			{
				IsSuccess = false,
				Message = "Could not get the assets for the specified version"
			};
		}

		private async Task<MCAssets> CreateMCVA(AssetMCVersion version, MinecraftEdition minecraftEdition)
		{
			MCAssets assets = await GenerateAssets(version, minecraftEdition);
			MinecraftVersionAssets mcva = new()
			{
				Name = assets.Name,
				Version = assets.Version,
				Edition = minecraftEdition == MinecraftEdition.Java ? "java" : "bedrock",
				JSON = JsonConvert.SerializeObject(assets, new JsonSerializerSettings
				{
					ContractResolver = contractResolver,
					Formatting = Formatting.None
				})
			};
			await _vaRepository.AddVersionAssets(mcva);
			return assets;
		}

		private string TrimBedrockReleaseId(string releaseName)
			=> releaseName.TrimStart('v').Replace("-preview", "");

		private List<AssetMCVersion> LimitVersions(List<AssetMCVersion> versions, bool bypassHighestPatchCheck = false)
		{
			List<AssetMCVersion> newList = new();
			DateTime OldestDateTime = DateTime.Parse("2013-04-24T15:45:00+00:00");

			// Select all full-release versions >=1.5.2 (and the latest snapshot)
			List<AssetMCVersion> tempList = versions.Where(x => x.ReleaseTime > OldestDateTime)
				.OrderByDescending(x => x.ReleaseTime).ToList();

			if (bypassHighestPatchCheck)
			{
				newList = tempList;
			}
			else
			{
				// This logic is very confusing. Limit to the highest patch (MAJOR.MINOR.PATCH) for each MAJOR.MINOR
				// release (e.g. 1.8.9, 1.17.1), with the exception of the latest snapshot / pre-release / RC.
				tempList.ToList().ForEach(x =>
				{
					List<string> split = x.Id.Split('.').ToList();
					// Include latest snapshot if it is newer than the latest full-release
					if ((split.Count == 1 || x.Id.Any(char.IsLetter)) && x.ReleaseTime >= tempList[0].ReleaseTime)
						newList.Add(x);
					else
					{
						List<int> verSplit = split.Select(int.Parse).ToList();
						if (!newList.Any(y => y.Id.StartsWith($"{verSplit[0]}.{verSplit[1]}") && y.Type == "release"))
							newList.Add(x);
					}
				});
			}
			return newList;
		}

		private async Task<string> GetJarDownload(AssetMCVersion version)
		{
			if (version.Edition != "java")
				return string.Empty;

			string url = version.Url;
			using var httpResponse = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
			httpResponse.EnsureSuccessStatusCode();

			var rawJson = await httpResponse.Content.ReadAsStringAsync();
			var json = JObject.Parse(rawJson);

			JToken? downloadUrl = json.SelectToken("$.downloads.client.url");
			string dUrl = downloadUrl != null ? downloadUrl.ToString() : "";
			return dUrl;
		}

		private async Task<bool> DownloadJar(AssetMCVersion version, string path)
		{
			try
			{
				string url = await GetJarDownload(version);
				if (string.IsNullOrWhiteSpace(url))
					return false;

				return await DownloadFile(url, path);
			}
			catch (Exception)
			{
				return false;
			}
		}

		private async Task<bool> DownloadFile(string url, string path)
		{
			try
			{
				HttpResponseMessage response = await _httpClient.GetAsync(url);

				if (!response.IsSuccessStatusCode) return false;

				Stream stream = await response.Content.ReadAsStreamAsync();
				await using FileStream fileStream = new(path, FileMode.Create);
				await stream.CopyToAsync(fileStream);
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Failed to download file");
				return false;
			}
		}

		private List<string> GetFileListFromJar(string jarPath)
		{
			List<string> files = new();
			using ZipArchive zip = ZipFile.OpenRead(jarPath);
			foreach (var file in zip.Entries)
			{
				if (file.FullName.EndsWith("png"))
					files.Add(file.FullName);
			}
			zip.Dispose();
			return files;
		}

		private List<string> GetFileListFromBedrockZip(string bedrockZip)
		{
			List<string> files = new();
			using ZipArchive zip = ZipFile.OpenRead(bedrockZip);
			foreach (var file in zip.Entries.Where(x => Regex.IsMatch(x.FullName, BEDROCK_ZIP_REGEX)))
			{
				string fileName = Regex.Replace(file.FullName, BEDROCK_ZIP_REGEX, string.Empty).TrimStart('/', '\\');
				if (fileName.EndsWith("png") || fileName.EndsWith("tga"))
					files.Add(fileName);
			}
			zip.Dispose();
			return files;
		}

		private async Task<MCAssets> GenerateAssets(AssetMCVersion version, MinecraftEdition minecraftEdition)
		{
			MCAssets mcAssets = new();

			Guid downloadFolder = Guid.NewGuid();
			string assetGenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AssetGeneration");
			string path = Path.Combine(assetGenPath, downloadFolder.ToString());
			Directory.CreateDirectory(path);
			string file = Path.Combine(path, "temp.zip");

			if (minecraftEdition == MinecraftEdition.Java ? await DownloadJar(version, file) : await DownloadFile(version.Url, file))
			{
				List<string> textures = minecraftEdition == MinecraftEdition.Java ? GetFileListFromJar(file) : GetFileListFromBedrockZip(file);

				if (textures is { Count: > 0 })
				{
					mcAssets = new()
					{
						Name = version.Id,
						Version = ASSET_VERSION,
						CreatedDate = DateTime.UtcNow,
						Minecraft = new()
						{
							Version = version.Id,
							Type = version.Type,
							Edition = minecraftEdition == MinecraftEdition.Java ? "java" : "bedrock",
							ReleaseTime = version.ReleaseTime
						},
						Textures = textures
					};
				}
			}
			else
			{
				_logger.LogError($"Failed to download {version.Id} assets!");
			}

			if (Directory.Exists(path))
				Directory.Delete(path, true);

			return mcAssets;
		}
	}

	public interface IToolsLogic
	{
		Task<List<AssetMCVersion>> GetJavaMCVersions(bool bypassHighestVersionLimit = false);
		Task<List<AssetMCVersion>> GetBedrockMCVersions();
		Task<ResponseModel<MCAssets>> GetMinecraftJavaAssets(string version, bool bypassHighestVersionLimit = false);
		Task<ResponseModel<MCAssets>> GetMinecraftBedrockAssets(string version);
		Task<bool> PregenerateJavaAssets(List<AssetMCVersion>? versions = null, bool bypassHighestVersionLimit = false);
		Task<bool> PregenerateBedrockAssets(List<AssetMCVersion>? versions = null);
		Task<ResponseModel<string>> GetMinecraftJavaJar(string version);
		Task PurgeAssets();
	}
}