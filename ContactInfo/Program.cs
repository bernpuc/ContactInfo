using System.Diagnostics;
using ContactInfo.Components;
using ContactInfo.Models;
using ContactInfo.Services;
using ContactInfo.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(1));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<IUserSettingsService, UserSettingsService>();
builder.Services.AddSingleton<IExcelService, ExcelService>();
builder.Services.AddScoped<BatchProcessorService>();

// Register each lookup source — add more here as new providers are integrated
builder.Services.AddHttpClient<IContactSource, ApolloService>();
builder.Services.AddHttpClient<IContactSource, RocketReachService>();
builder.Services.AddHttpClient<IContactSource, SignalHireService>();

// Demo sources — remove when real providers are configured
builder.Services.AddSingleton<IContactSource>(new DemoContactSource("DemoA", variant: 1));
builder.Services.AddSingleton<IContactSource>(new DemoContactSource("DemoB", variant: 2));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // Open the browser automatically when the server is ready
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5100";
        Task.Run(() =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* best-effort */ }
        });
    });
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
