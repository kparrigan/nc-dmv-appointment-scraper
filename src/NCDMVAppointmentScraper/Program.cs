using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCDMVAppointmentScraper;
using NLog;
using NLog.Extensions.Logging;
using OpenQA.Selenium.Chrome;

LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("NLog.config");

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    builder.AddNLog();
});

services.AddTransient<Worker>();
services.AddScoped<IAppointmentService, AppointmentService>();
services.AddScoped<ScraperConfig, ScraperConfig>();
services.AddScoped<ChromeDriver, ChromeDriver>();

using var serviceProvider = services.BuildServiceProvider();

var worker = serviceProvider.GetRequiredService<Worker>();
await worker.Work();

NLog.LogManager.Shutdown();