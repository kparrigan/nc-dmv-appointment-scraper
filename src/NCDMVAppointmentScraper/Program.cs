using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCDMVAppointmentScraper;
using NLog;
using NLog.Extensions.Logging;
using OpenQA.Selenium.Chrome;
using Quartz;
using Quartz.Logging;

    var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .Build();

    LogManager.Configuration = new NLogLoggingConfiguration(configuration.GetSection("NLog"));

    var logger = LogManager.GetCurrentClassLogger();

    try
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                    builder.AddNLog();
                });

                services.AddSingleton<IConfiguration>(configuration);
                services.AddSingleton<Worker>();
                services.AddScoped<IAppointmentService, AppointmentService>();
                services.AddScoped<IWebDriverFactory, WebDriverFactory>();
                services.AddScoped<IEmailService, EmailService>();

                services.AddSingleton(provider =>
                    ScraperConfig.LoadFromConfiguration(provider.GetRequiredService<IConfiguration>())
                );

                services.AddSingleton(provider =>
                    EmailConfig.LoadFromConfiguration(provider.GetRequiredService<IConfiguration>())
                );


                var cronSchedule = configuration["Quartz:CronSchedule"];

                services.AddQuartz(q =>
                {
                    q.UseMicrosoftDependencyInjectionJobFactory();

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

                    services.AddQuartzHostedService(options =>
                    {
                        options.AwaitApplicationStarted = true;
                        options.WaitForJobsToComplete = true;
                    });
                });
            })
            .UseWindowsService();

        logger.Info("Intializing Service.");

#if DEBUG
        await builder.RunConsoleAsync();
#else
        await builder.Build().RunAsync();
#endif
    }
    catch (Exception ex)
    {
        logger.Error(ex, "Stopped program because of exception");
    }
    finally
    {
        LogManager.Shutdown();
    }