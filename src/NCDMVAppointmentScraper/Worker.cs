using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Security.Authentication;

namespace NCDMVAppointmentScraper
{
    internal class Worker(ScraperConfig scraperConfig, ILogger<Worker> logger, IAppointmentService appointmentSerice, IEmailService emailService)
    {
        private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAppointmentService _appointmentSerice = appointmentSerice ?? throw new ArgumentNullException(nameof(appointmentSerice));
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly ScraperConfig _scraperConfig = scraperConfig ?? throw new ArgumentNullException(nameof(scraperConfig));

        public async Task Work()
        {
            _logger.LogInformation("Beginning Processing.");

            try
            {
                var appointments = _appointmentSerice.GetAppointments();

                _logger.LogInformation($"Found {appointments.Count} appointments.");

                appointments = appointments.Where(a => 
                    _scraperConfig.LocationsToMonitor.Contains(a.LocationName, StringComparer.OrdinalIgnoreCase)).ToList();

                _logger.LogInformation($"Found {appointments.Count} appointments at monitored locations.");

                foreach (var appointment in appointments)
                {
                    _logger.LogInformation("Location: {Location}, Appointment Date: {Date}", appointment.LocationName, appointment.AppointmentDate.ToShortDateString());
                }

                if (appointments.Any())
                {
                    appointments = appointments.OrderBy(a => a.AppointmentDate).ToList();
                    _emailService.SendEmail(appointments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing.");
            }

            _logger.LogInformation("Processing Complete.");
        }
    }
}
