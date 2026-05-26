using CovenantCouncil.App.Services;
using CovenantCouncil.App.Views;
using CovenantCouncil.Core;
using CovenantCouncil.Infrastructure;
using CovenantCouncil.ViewModels;
using CovenantCouncil.ViewModels.Certificates;
using CovenantCouncil.ViewModels.Licenses;
using CovenantCouncil.ViewModels.Parties;
using CovenantCouncil.ViewModels.Settings;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ReactiveUI.Builder;

namespace CovenantCouncil.App;

public static class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {
    var builder = MauiApp.CreateBuilder();
    builder
      .UseMauiApp<App>()
      .UseReactiveUI(reactiveUI => reactiveUI.WithMainThreadScheduler(
        MauiReactiveUIBuilderExtensions.MauiMainThreadScheduler,
        setRxApp: true))
      .ConfigureFonts(fonts =>
      {
      });

    var logSink = new RollingFileLogSink(AppLogPaths.LogDirectory);
    builder.Services.AddSingleton(logSink);
    builder.Services.AddSingleton<AppDiagnosticsService>();
    builder.Logging.AddProvider(new RollingFileLoggerProvider(logSink));
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    builder.Services.AddCoreServices();
    builder.Services.AddInfrastructureServices();
    builder.Services.AddViewModelServices();
    builder.Services.AddSingleton<IDatabaseFilePicker, DatabaseFilePicker>();
    builder.Services.AddSingleton<AppShell>();
    builder.Services.AddTransient<DatabaseGatePage>();
    builder.Services.AddTransient<SettingsPage>();
    builder.Services.AddTransient<PartiesPage>();
    builder.Services.AddTransient<AddPartyPage>();
    builder.Services.AddTransient<CertificatesPage>();
    builder.Services.AddTransient<LicensesPage>();
    builder.Services.AddTransient<IssueLicensePage>();

    var otlpEndpoint = Environment.GetEnvironmentVariable("COVENANTCOUNCIL_OTLP_ENDPOINT");
    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
      builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
          .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CovenantCouncil.App"))
          .AddSource("CovenantCouncil")
          .AddSource("Microsoft.Extensions.Logging")
          .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
        .WithMetrics(metrics => metrics
          .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CovenantCouncil.App"))
          .AddMeter("CovenantCouncil")
          .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));
    }

#if DEBUG
    builder.Logging.AddDebug();
#endif

    return builder.Build();
  }
}
