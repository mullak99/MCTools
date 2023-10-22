using AspNetCoreRateLimit;
using Auth0.AuthenticationApi;
using MCTools.API.Abstractions;
using MCTools.API.Cache;
using MCTools.API.Extentions;
using MCTools.API.Logging;
using MCTools.API.Logic;
using MCTools.API.Models;
using MCTools.API.Repository;
using MCTools.API.Services;
using MCTools.SDK.Enums.Telemetry;
using MCTools.SDK.Models.Telemetry;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Octokit;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace MCTools.API
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
			AuthConfig = Configuration.GetSection("Auth0").Get<Auth0Config>();
		}

		public GlobalSettings? GlobalSettings { get; set; }
		public Auth0Config AuthConfig { get; set; }
		public IConfiguration Configuration { get; }
		private CurrentUserService? _currentUserService;

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddDefaultPolicy(builder =>
				{
					builder.WithOrigins(Configuration["AllowedHosts"])
						.AllowAnyMethod()
						.AllowAnyHeader()
						.WithHeaders("Authorization");
				});
			});
			services.AddHealthChecks();
			services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
			services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
			services.AddMemoryCache();
			services.AddResponseCaching();
			services.AddRouting(options => options.LowercaseUrls = true);
			services.AddControllers().AddNewtonsoftJson(options =>
			{
				options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
			});

			services.AddAuthenticationWithAuth0(AuthConfig);

			var managementApiAudience = AuthConfig.Audience;
			services.AddSingleton(managementApiAudience);

			services.AddSingleton(AuthConfig);

			services.AddLogging(builder =>
			{
				builder.ClearProviders(); // Remove built-in providers
				builder.AddProvider(new LoggerProvider(Configuration));
			});

			services.AddScoped<IAuthenticationApiClient>(_ => new AuthenticationApiClient(AuthConfig.Authority.Replace("https://", "")));

			services.AddSingleton<IMongoClient>(_ =>
			{
				MongoClientSettings? clientSettings = MongoClientSettings.FromConnectionString(Configuration.GetConnectionString("MongoDb"));
				clientSettings.MaxConnectionPoolSize = 1000;
				return new MongoClient(clientSettings);
			});

			services.AddSingleton(s => {
				var client = s.GetRequiredService<IMongoClient>();
				var database = client.GetDatabase("MCTools");
				return database;
			});

			services.AddHttpClient();
			services.Configure<HttpClientFactoryOptions>(options =>
			{
				options.HttpClientActions.Add(client =>
				{
					client.DefaultRequestHeaders.Add("User-Agent", Configuration["Common:UserAgent"]);
				});
			});

			GitOptions gitOptions = new()
			{
				PersonalAccessToken = Configuration["Tokens:GitHub"],
				AuthorName = Configuration["Common:Name"],
				AuthorEmail = Configuration["Common:Email"]
			};

			GitHubClient githubClient = new(new ProductHeaderValue(Configuration["Common:UserAgent"]))
			{
				Credentials = new(gitOptions.PersonalAccessToken)
			};

			GlobalSettings = new(Configuration);

			BsonClassMap.RegisterClassMap<AppInfo>(cm =>
			{
				cm.AutoMap();
				cm.SetIgnoreExtraElements(true);
			});

			services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
			services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
			services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
			services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();


			services.AddSingleton<IGitOptions>(gitOptions);
			services.AddSingleton<IGitHubClient>(githubClient);
			services.AddSingleton(GlobalSettings);

			// Cache
			services.AddSingleton<IVersionAssetsCache, VersionAssetsCache>();

			// Repos
			services.AddScoped<IVersionAssetsRepository, VersionAssetsRepository>();
			services.AddScoped<ITelemetryRepository, TelemetryRepository>();

			// Logic
			services.AddScoped<IToolsLogic, ToolsLogic>();
			services.AddScoped<IConversionLogic, ConversionLogic>();
			services.AddScoped<ITelemetryLogic, TelemetryLogic>();

			// Autorun
			services.AddHostedService<ScheduledService>();

			BuildServiceProviderAsync(services).Wait();

			services.AddApiVersioning(options =>
			{
				options.ReportApiVersions = true;
				options.DefaultApiVersion = new(1, 0);
				options.AssumeDefaultVersionWhenUnspecified = true;
				options.ApiVersionReader = new MediaTypeApiVersionReader();
			});

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1.0", new() { Title = "MCTools", Version = "v1.0" });
				c.AddSecurityDefinition("Bearer", new()
				{
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.OAuth2,
					Flows = new()
					{
						Implicit = new()
						{
							AuthorizationUrl = new($"{AuthConfig.Authority}/authorize?audience={AuthConfig.Audience}"),
							Scopes = new Dictionary<string, string>
							{
								{ "openid", "OpenID" },
								{ "profile", "Profile" }
							}
						}
					}
				});
				c.OperationFilter<SecurityRequirementsOperationFilter>();
				c.UseAllOfToExtendReferenceSchemas();

				c.MapType<AppReleaseType>(() => new OpenApiSchema
				{
					Type = "string",
					Enum = Enum.GetValues(typeof(AppReleaseType))
						.Cast<AppReleaseType>()
						.Select(e => new OpenApiString(e.ToString()))
						.ToList<IOpenApiAny>()
				});
			});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.DefaultModelRendering(ModelRendering.Model);
				c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "MCTools.API");
				c.OAuthClientId(AuthConfig.ClientId);
			});

			app.UseHttpsRedirection();
			app.UseResponseCaching();
			app.UseIpRateLimiting();
			app.UseRouting();
			app.UseCors();
			app.UseAuthentication();

			app.UsePermissions(_currentUserService!.GetPermissions());

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapHealthChecks("/health");
				endpoints.MapControllers();
			});
		}

		private async Task BuildServiceProviderAsync(IServiceCollection services)
		{
			var provider = services.BuildServiceProvider();
			var configuration = provider.GetService<IConfiguration>();

			if (configuration != null)
			{
				_currentUserService = await CurrentUserService.CreateAsync(configuration, provider.GetRequiredService<ILogger<CurrentUserService>>());
				services.AddSingleton<IRoleValidator>(_currentUserService);
				services.AddSingleton<IPermissionValidator>(_currentUserService);
				services.AddSingleton<ICurrentUserService>(_currentUserService);

				services.AddPermissionBasedAuthorizationWithPermissions(_currentUserService.GetPermissions());
			}
		}
	}
}
