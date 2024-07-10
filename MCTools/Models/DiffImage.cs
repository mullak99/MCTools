namespace MCTools.Models
{
	public class DiffImage
	{
		public string Name { get; set; }
		public byte[] Image { get; set; }
		public DiffImageType Type { get; set; }

		public DiffImage(string name, byte[] image, DiffImageType type)
		{
			Name = name;
			Image = image;
			Type = type;
		}

		public string ImageBase64 => $"data:image/png;base64,{System.Convert.ToBase64String(Image)}";
	}

	public enum DiffImageType
	{
		None = 0,
		Added = 1,
		Removed = 2,
		Different = 3
	}
}
