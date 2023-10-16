using MCTools.Enums;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCTools.Models
{
	public class ResourcePack
	{
		public string Name { get; set; }
		public long Size { get; set; }
		public Assets BaseAssets { get; set; }
		public List<Assets> Overlays { get; set; }
		public bool IsProcessed { get; set; }

		private static string[] _imageFileTypes = { "png", "tga" };

		private readonly IBrowserFile _packFile;
		private readonly MCEdition _selectedEdition;

		public ResourcePack(IBrowserFile file, MCEdition selectedEdition)
		{
			_packFile = file;
			_selectedEdition = selectedEdition;
			Name = file.Name;
			Size = file.Size;
		}

		public async Task Init(int maxBytes)
		{
			IsProcessed = false;
			await using MemoryStream ms = new(maxBytes);
			await _packFile.OpenReadStream(maxBytes).CopyToAsync(ms);
			using ZipArchive zip = new(ms);

			BaseAssets = new Assets()
			{
				Name = "Assets",
				Textures = new List<string>(),
				McMetas = new List<string>()
			};
			Overlays = new List<Assets>();

			if (_selectedEdition == MCEdition.Java)
			{
				ZipArchiveEntry mcMetaEntry = zip.Entries.FirstOrDefault(x => x.FullName.ToLower() == "pack.mcmeta");
				if (mcMetaEntry != null)
				{
					using StreamReader reader = new(mcMetaEntry.Open(), System.Text.Encoding.UTF8);
					List<string> folders = GetOverlaysFromMcMeta(await reader.ReadToEndAsync());

					List<string> folderNames = zip.Entries
						.Where(entry => entry.FullName.EndsWith("/")) // Filter entries that are folders
						.Select(entry => GetDirectoryName(entry.FullName))
						.Distinct()
						.Intersect(folders)
						.ToList();

					if (folderNames.Any(x => x != "assets"))
					{
						foreach (string folderName in folderNames.Where(x => x != "assets"))
						{
							Overlays.Add(new Assets()
							{
								Name = folderName,
								Textures = new List<string>(),
								McMetas = new List<string>()
							});
						}
					}
				}
			}
		}

		private List<string> GetOverlaysFromMcMeta(string jsonText)
		{
			try
			{
				using JsonDocument doc = JsonDocument.Parse(jsonText);
				JsonElement root = doc.RootElement;
				JsonElement entries = root.GetProperty("overlays").GetProperty("entries");
				return entries.EnumerateArray().Select(entry => entry.GetProperty("directory").GetString()).Where(directory => !string.IsNullOrEmpty(directory)).ToList();
			}
			catch (Exception)
			{
				// Overlays doesn't exist in mcmeta
				return new List<string>();
			}
		}

		public async Task Process(int maxBytes)
		{
			if (BaseAssets == null)
				await Init(maxBytes);

			await using MemoryStream ms = new(maxBytes);
			await _packFile.OpenReadStream(maxBytes).CopyToAsync(ms);
			using ZipArchive zip = new(ms);

			foreach (ZipArchiveEntry file in zip.Entries)
			{
				string fileName = GetFileName(file.FullName, _selectedEdition);
				if (fileName != null)
				{
					string folderName = GetDirectoryName(file.FullName);
					string ext = Path.GetExtension(fileName).ToLower().Replace(".", string.Empty);
					if ((_selectedEdition == MCEdition.Java && folderName == "assets") || (_selectedEdition == MCEdition.Bedrock && folderName == "textures") || string.IsNullOrWhiteSpace(folderName))
					{
						if (_imageFileTypes.Contains(ext))
							BaseAssets?.Textures.Add(fileName);
						else if (ext == "mcmeta" && fileName != "pack.mcmeta")
							BaseAssets?.McMetas.Add(fileName);
					}
					else
					{
						Assets overlay = Overlays.FirstOrDefault(x => x.Name == folderName);
						if (overlay == null)
							continue;

						if (_imageFileTypes.Contains(ext))
							overlay.Textures.Add(fileName.Replace($"{folderName}/", string.Empty));
						else if (ext == "mcmeta")
							overlay.McMetas.Add(fileName.Replace($"{folderName}/", string.Empty));
					}
				}
			}
			IsProcessed = true;
		}

		public List<string> GetTextures()
		{
			List<string> textures = new();
			textures.AddRange(BaseAssets.Textures);
			foreach (Assets overlay in Overlays.Where(x => x.Enabled))
				textures.AddRange(overlay.Textures);
			return textures.Distinct().ToList();
		}

		public string GetTexturePath(string path)
		{
			if (BaseAssets.Textures.Contains(path))
				return Path.Combine(path);

			string overlayPath = Overlays.FirstOrDefault(x => x.Enabled && x.Textures.Contains(path))?.Name;
			return overlayPath != null ? Path.Combine(overlayPath, path) : null;
		}

		public List<string> GetMcMetas()
		{
			List<string> mcMetas = new();
			mcMetas.AddRange(BaseAssets.McMetas);
			foreach (Assets overlay in Overlays.Where(x => x.Enabled))
				mcMetas.AddRange(overlay.McMetas);
			return mcMetas.Distinct().ToList();
		}

		public IBrowserFile GetFile()
			=> _packFile;

		private string GetFileName(string fileName, MCEdition selectedEdition)
		{
			if (fileName.EndsWith("png") || (selectedEdition == MCEdition.Java && fileName.EndsWith("mcmeta")) || (selectedEdition == MCEdition.Bedrock && fileName.EndsWith("tga")))
				return fileName;
			return null;
		}

		private string GetDirectoryName(string fullPath)
		{
			string[] split = fullPath.Split('/');
			return split.Length <= 1 ? null : split.First();
		}
	}

	public class Assets
	{
		public string Name { get; set; }
		public List<string> Textures { get; set; } = new();
		public List<string> McMetas { get; set; } = new();
		public bool Enabled { get; set; } = true;
	}
}
