namespace MCTools.API.Models
{
	public class Auth0Config
	{
		/// <summary>
		/// The authority URL taken from the setup
		/// in Auth0.
		/// </summary>
		public string Authority { get; set; } = string.Empty;

		/// <summary>
		/// The Audience specified in Auth0 Configuration
		/// </summary>
		public string Audience { get; set; } = string.Empty;

		/// <summary>
		/// The Client Id for the application
		/// </summary>
		public string ClientId { get; set; } = string.Empty;

		/// <summary>
		/// The Client Secret for the application
		/// </summary>
		public string ClientSecret { get; set; } = string.Empty;
	}
}
