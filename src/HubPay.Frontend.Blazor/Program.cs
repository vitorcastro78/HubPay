using HubPay.Frontend.Blazor;
using HubPay.Frontend.Blazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = ResolveApiBaseUrl(builder.Configuration);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<HubPayApiClient>();
builder.Services.AddScoped<TransactionHubClient>();

await builder.Build().RunAsync();

static string ResolveApiBaseUrl(IConfiguration configuration)
{
    var url = configuration["ApiBaseUrl"]
              ?? configuration["services:webapi:https:0"]
              ?? configuration["services:webapi:http:0"]
              ?? "https://localhost:7239/";

    return url.EndsWith('/') ? url : url + "/";
}
