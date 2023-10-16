using System.ComponentModel;
using System.Reflection;

namespace MCTools.SDK.Extensions
{
	public static class EnumExtensions
	{
		public static string GetDescription(this Enum value)
		{
			FieldInfo? fieldInfo = value.GetType().GetField(value.ToString());
			DescriptionAttribute[]? attributes = (DescriptionAttribute[]?)fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false);

			return attributes is { Length: > 0 } ? attributes[0].Description : value.ToString();
		}
	}
}
