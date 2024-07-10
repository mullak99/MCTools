using Newtonsoft.Json;

namespace MCTools.SDK.Models
{
	public class ResponseModel<T>
	{
		[JsonProperty("isSuccess")]
		public bool IsSuccess { get; set; } = true;

		[JsonProperty("message")]
		public string Message { get; set; } = "";

		[JsonProperty("data")]
		public T? Data { get; set; }
	}
}
