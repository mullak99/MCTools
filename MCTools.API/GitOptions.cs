namespace MCTools.API
{
	public class GitOptions : IGitOptions
	{
		public string PersonalAccessToken { get; set; } = string.Empty;
		public string AuthorName { get; set; } = string.Empty;
		public string AuthorEmail { get; set; } = string.Empty;
	}

	public interface IGitOptions
	{
		string PersonalAccessToken { get; }
		string AuthorName { get; }
		string AuthorEmail { get; }
	}
}
