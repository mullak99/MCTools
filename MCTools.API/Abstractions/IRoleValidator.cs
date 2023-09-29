using System.Security.Claims;

namespace MCTools.API.Abstractions
{
	public interface IRoleValidator
	{
		bool CurrentUserHasRole(ClaimsIdentity user, string name);
	}
}
