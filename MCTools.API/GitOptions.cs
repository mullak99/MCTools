namespace MCTools.API
{
	public class GitOptions : IGitOptions
	{
		public string PersonalAccessToken { get; set; }
		public string AuthorName { get; set; }
		public string AuthorEmail { get; set; }
	}

	public interface IGitOptions
	{
		string PersonalAccessToken { get; }
		string AuthorName { get; }
		string AuthorEmail { get; }
	}
}
