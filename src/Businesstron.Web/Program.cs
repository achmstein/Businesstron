using System.Text.Json.Serialization;
using Businesstron.Application;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Infrastructure;
using Businesstron.Infrastructure.Data;
using Businesstron.Infrastructure.Identity;
using Businesstron.Web.Endpoints;
using Businesstron.Web.Infrastructure;
using Businesstron.Web.Services;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Writable overrides layer for runtime-mutable settings (Ontraport + 2Captcha
// credentials edited from the UI). Lives outside the immutable container image so
// SettingsService can persist admin changes; reloadOnChange flushes the
// IOptionsMonitor/IOptionsSnapshot consumers automatically, so edits take effect
// without a restart. Added last, so it overrides appsettings.* for the keys it sets.
var overridesPath = builder.Configuration["Storage:OverridesPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "settings.overrides.json");
builder.Configuration.AddJsonFile(overridesPath, optional: true, reloadOnChange: true);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUser, CurrentUser>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Bind enums (e.g. SearchSource) from JSON strings both ways.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "https://localhost:5173"];

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseExceptionHandler();

// Serve the built React SPA (populated in wwwroot on publish; empty in dev where Vite serves it).
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});

app.MapGroup("/api").MapIdentityApi<ApplicationUser>();

app.MapSearchRunsEndpoints();
app.MapFilterKeywordsEndpoints();
app.MapSettingsEndpoints();
app.MapAccountEndpoints();
app.MapUsersEndpoints();

app.MapDefaultEndpoints();

// Client-side routing fallback (serves index.html for non-API routes in production).
app.MapFallbackToFile("index.html");

app.Run();
