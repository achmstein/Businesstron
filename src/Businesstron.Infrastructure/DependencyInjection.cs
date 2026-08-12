using Businesstron.Application.Common.Interfaces;
using Businesstron.Infrastructure.Csv;
using Businesstron.Infrastructure.Data;
using Businesstron.Infrastructure.Data.Interceptors;
using Businesstron.Infrastructure.ExternalClients.Abr;
using Businesstron.Infrastructure.ExternalClients.Asic;
using Businesstron.Infrastructure.ExternalClients.Captcha;
using Businesstron.Infrastructure.ExternalClients.DataGov;
using Businesstron.Infrastructure.Identity;
using Businesstron.Infrastructure.Jobs;
using Businesstron.Infrastructure.Ontraport;
using Businesstron.Infrastructure.Services;
using Businesstron.Infrastructure.Settings;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Businesstron.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BusinesstronDb")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No connection string ('BusinesstronDb' or 'DefaultConnection') was found.");

        // --- Persistence ---
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(
                maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitialiser>();

        // --- Identity (cookie auth + minimal API endpoints) ---
        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddAuthorization();

        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        services.AddSingleton<IEmailSender<ApplicationUser>, NoOpEmailSender>();

        // --- Options ---
        services.Configure<AsicOptions>(configuration.GetSection(AsicOptions.SectionName));
        services.Configure<TwoCaptchaOptions>(configuration.GetSection(TwoCaptchaOptions.SectionName));
        services.Configure<DataGovOptions>(configuration.GetSection(DataGovOptions.SectionName));
        services.Configure<OntraportOptions>(configuration.GetSection(OntraportOptions.SectionName));

        // --- External clients ---
        services.AddSingleton<ICaptchaSolver, TwoCaptchaSolver>();
        // Enrichment runs several ASIC sessions in parallel; each worker creates its own
        // client from this factory (a session's cookie jar + ADF state are per-instance).
        services.AddSingleton<IAsicRegistryClientFactory, AsicRegistryClientFactory>();

        services.AddHttpClient<IDataGovClient, DataGovClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataGovOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddHttpClient<IAbrClient, AbrClient>(http =>
        {
            http.BaseAddress = new Uri("https://abr.business.gov.au/");
            http.Timeout = TimeSpan.FromSeconds(120);
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0");
        });

        services.AddHttpClient<IOntraportClient, OntraportClient>();

        services.AddSingleton<ICsvExporter, CsvExporter>();

        // UI-editable integration credentials (persisted to the overrides file).
        services.AddSingleton<ISettingsService, SettingsService>();

        // --- Background jobs ---
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options => options.WorkerCount = 3);

        services.AddScoped<ISearchProcessingService, SearchProcessingService>();
        services.AddScoped<IJobScheduler, HangfireJobScheduler>();

        return services;
    }
}
