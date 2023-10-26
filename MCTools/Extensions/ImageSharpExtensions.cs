using SixLabors.ImageSharp.PixelFormats;

namespace MCTools.Extensions
{
	public static class ImageSharpExtensions
	{
		public static string ToHexNoAlpha(this Rgba32 color)
			=> $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}
