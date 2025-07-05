using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MCTools.API.Abstractions;
using MCTools.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MCTools.API.Extentions
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// This method will configure the services collection with the necessary setup details for Auth0 authentication
		/// using the Auth0 Config object.
		/// </summary>
		/// <param name="services">Services Collection</param>
		/// <param name="config"><see cref="Auth0Config"/></param>
		public static void AddAuthenticationWithAuth0(this IServiceCollection services, Auth0Config config)
		{
			services.AddAuthentication(options =>
				{
					options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
					options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
					options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				})
				.AddJwtBearer(options =>
				{
					options.Authority = config.Authority;
					options.Audience = config.Audience;
					options.RequireHttpsMetadata = false;
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuerSigningKey = true,
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.ClientSecret)),
						ValidateIssuer = true,
						ValidIssuer = config.Authority,
						ValidateAudience = true,
						ValidAudience = config.Audience,
						RoleClaimType = "https://schemas.microsoft.com/ws/2008/06/identity/claims/role"
					};
					options.Events = new JwtBearerEvents
					{
						OnTokenValidated = context =>
						{
							if (context.SecurityToken is not JwtSecurityToken token)
								return Task.CompletedTask;
							if (context.Principal?.Identity is ClaimsIdentity identity)
								identity.AddClaim(new Claim("access_token", token.RawData));

							return Task.CompletedTask;
						}
					};
				});
		}

		/// <summary>
		/// This will add policies that check for claims to ensure that certain roles are present.  It allows the
		/// use of policies that check for multiple roles.
		/// </summary>
		/// <param name="services">Services Collection</param>
		/// <param name="rolePolicies">Key = Policy Name, and Value = List of Role Names</param>
		public static void AddRoleBasedAuthorizationWithRoles(this IServiceCollection services,
			Dictionary<string, List<string>> rolePolicies)
		{
			services.AddAuthorization(options =>
			{
				foreach (var rolePolicy in rolePolicies)
				{
					options.AddPolicy(rolePolicy.Key, policy => policy.RequireClaim(ClaimTypes.Role, rolePolicy.Value));
				}
			});
		}

		/// <summary>
		/// This will add policies that check for specific permissions in the identities claim.  You can then specify
		/// the permission required for each action in your application.
		/// </summary>
		/// <param name="services">Services Collection</param>
		/// <param name="permissions">List of Permissions</param>
		public static void AddPermissionBasedAuthorizationWithPermissions(this IServiceCollection services,
			List<string> permissions)
		{
			services.AddAuthorization(options =>
			{
				foreach (var permission in permissions)
				{
					options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
				}
			});
		}


		/// <summary>
		/// When using custom claims in Auth0 Rules this will allow you to remap an email claim.  This must
		/// be called after UseAuthentication and before UseAuthorization.
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/></param>
		/// <param name="name">Custom Claim Name</param>
		public static void UseCustomEmailClaim(this IApplicationBuilder app, string name)
		{
			app.Use(async (context, next) =>
			{
				var userIdentity = context.User.Identities.FirstOrDefault();

				userIdentity?.AddClaim(new Claim(ClaimTypes.Email,
					userIdentity.Claims.FirstOrDefault(c => c.Type == name)?.Value ?? "unknown"));

				await next.Invoke();
			});
		}

		/// <summary>
		/// When using custom claims in Auth0 Rules this will allow you to remap an email claim.  This must
		/// be called after UseAuthentication and before UseAuthorization.
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/></param>
		/// <param name="name">Custom Claim Name</param>
		public static void UseCustomClaim(this IApplicationBuilder app, string mapTo, string mapFrom)
		{
			app.Use(async (context, next) =>
			{
				var userIdentity = context.User.Identities.FirstOrDefault();

				userIdentity?.AddClaim(new Claim(mapTo,
					userIdentity.Claims.FirstOrDefault(c => c.Type == mapFrom)?.Value ?? "unknown"));

				await next.Invoke();
			});
		}

		/// <summary>
		/// This will instruct your application to add roles claims to the default identity for each request.  This
		/// is required to allow for Role based Authorization.  This call must be made before calling UseAuthorization
		/// in the pipeline.  It also requires that the Services Collection contain a valid <see cref="IRoleValidator"/>
		/// instance.
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/></param>
		/// <param name="roles">List of Application Roles</param>
		public static void UseRoles(this IApplicationBuilder app, List<string> roles)
		{
			app.Use(async (context, next) =>
			{
				var userIdentity = context.User.Identities.FirstOrDefault();

				if (userIdentity != null)
				{
					var roleValidator =
						(IRoleValidator)context.RequestServices.GetService(typeof(IRoleValidator))!;

					foreach (var role in roles.Where(role => roleValidator?.CurrentUserHasRole(userIdentity, role) == true))
					{
						userIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
					}
				}

				await next.Invoke();
			});
		}

		/// <summary>
		/// This will instruct your application to add permission claims to the default identity for each request.  This
		/// is required to allow for Permission Based Authorization.  This call must be made before calling UseAuthorization
		/// in the ASP.Net pipeline.  It also requires that the Services Collection contain a valid <see cref="IRoleValidator"/>
		/// instance.
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/></param>
		/// <param name="permissions">List of Application Permissions</param>
		public static void UsePermissions(this IApplicationBuilder app, List<string> permissions)
		{
			app.Use(async (context, next) =>
			{
				var userIdentity = context.User.Identities.FirstOrDefault();

				if (userIdentity != null)
				{
					var permissionValidator =
						(IPermissionValidator)context.RequestServices.GetService(typeof(IPermissionValidator))!;

					foreach (var permission in permissions.Where(permission =>
						permissionValidator?.CurrentUserHasPermission(userIdentity, permission) == true))
					{
						userIdentity.AddClaim(new Claim("permission", permission));
					}
				}

				await next.Invoke();
			});
		}
	}
}
