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
			await using MemoryStream ms = new(maxBytes);
			await _packFile.OpenReadStream(maxBytes).CopyToAsync(ms);
			using ZipArchive zip = new(ms);

			BaseAssets = new Assets()
			{
				Name = "Assets",
				Textures = new List<string>()
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
								Textures = new List<string>()
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
					if (folderName == "assets" || string.IsNullOrWhiteSpace(folderName))
						BaseAssets?.Textures.Add(fileName);
					else
						Overlays.FirstOrDefault(x => x.Name == folderName)?.Textures.Add(fileName.Replace($"{folderName}/", ""));
				}
			}
		}

		public List<string> GetTextures()
		{
			List<string> textures = new();
			textures.AddRange(BaseAssets.Textures);
			foreach (Assets overlay in Overlays.Where(x => x.Enabled))
				textures.AddRange(overlay.Textures);
			return textures.Distinct().ToList();
		}

		private string GetFileName(string fileName, MCEdition selectedEdition)
		{
			if (fileName.EndsWith("png") || (selectedEdition == MCEdition.Bedrock && fileName.EndsWith("tga")))
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
		public List<string> Textures { get; set; }
		public bool Enabled { get; set; } = true;
	}
}
