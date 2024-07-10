namespace MCTools.Models
{
	public class AppLinks
	{
		public string GitHub { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;

		public string GitHubIssues
			=> $"{GitHub}/issues";
	}
}
