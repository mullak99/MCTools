using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using MCTools.Controllers;
using Microsoft.Extensions.Configuration;
using MudBlazor;

namespace MCTools
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddMudServices(config =>
                {
                    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
                    config.SnackbarConfiguration.PreventDuplicates = false;
                    config.SnackbarConfiguration.NewestOnTop = false;
                    config.SnackbarConfiguration.ShowCloseIcon = true;
                    config.SnackbarConfiguration.VisibleStateDuration = 5000;
                    config.SnackbarConfiguration.HideTransitionDuration = 250;
                    config.SnackbarConfiguration.ShowTransitionDuration = 250;
                    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
                });

            builder.Services.AddBlazoredLocalStorage();

            var environment = builder.Configuration.GetValue<string>("Application:Environment");
            var url = builder.Configuration.GetValue<string>($"Endpoint:{environment}");
            Console.WriteLine($"API Endpoint: {url}");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(url) });
            builder.Services.AddScoped<ApiController>();

            await builder.Build().RunAsync();
        }
    }
}
