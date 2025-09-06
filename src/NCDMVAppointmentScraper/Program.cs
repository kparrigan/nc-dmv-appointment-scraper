using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCDMVAppointmentScraper;
using NLog;
using NLog.Extensions.Logging;
using OpenQA.Selenium.Chrome;
using Quartz;

LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("NLog.config");

var configBuildet = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var configuration = configBuildet.Build();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var cronSchedule = configuration["Quartz:CronSchedule"];

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            builder.AddNLog();
        });

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<Worker>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ChromeDriver, ChromeDriver>();

        services.AddSingleton(provider =>
            ScraperConfig.LoadFromConfiguration(provider.GetRequiredService<IConfiguration>())
        );

        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            // Define the job and trigger
            var jobKey = new JobKey("WorkerJob");
            q.AddJob<WorkerJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("WorkerJob-immediate-trigger")
                .StartNow());

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("WorkerJob-trigger")
                .WithCronSchedule(cronSchedule));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    });

await builder.RunConsoleAsync();

NLog.LogManager.Shutdown();