using System.Security.Claims;

namespace MCTools.API.Abstractions
{
	public interface IPermissionValidator
	{
		bool CurrentUserHasPermission(ClaimsIdentity user, string permission);
	}
}
