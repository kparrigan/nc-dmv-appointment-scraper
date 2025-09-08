using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Chrome;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal class EmailService(ILogger<EmailService> logger, EmailConfig config) : IEmailService
    {
        private readonly ILogger<EmailService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly EmailConfig _config = config ?? throw new ArgumentNullException(nameof(config));

        public async void SendEmail(List<AppointmentRecord> appointments)
        {
            if (appointments == null || !appointments.Any() )
            {
                throw new ArgumentException("No appointments to send in email.", nameof(appointments));
            }

            var apiKey = _config.ApiKey;
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_config.Sender);
            var subject = _config.Subject;
            var to = GetRecipients(_config.RecipientList);
            var htmlContent = BuildMessageBody(appointments);
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, to, subject, string.Empty, htmlContent);

            Response? response = null;

            try
            {
                response = await client.SendEmailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notifications.");
                throw;
            }

            if (response != null && response.StatusCode != HttpStatusCode.Accepted)
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError($"Error sending email notifications.{Environment.NewLine}" +
                    $"Response Code: {response.StatusCode}{Environment.NewLine}" +
                    $"Response Body: {responseBody}");
            }
        }

        private List<EmailAddress> GetRecipients(List<string> recipientAddresses) =>
            recipientAddresses.Select(email => new EmailAddress(email)).ToList();

        private string BuildMessageBody(List<AppointmentRecord> appointments)
        {
            var templatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), _config.TemplateFile);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Email template file not found: {templatePath}");
            }

            var template = File.ReadAllText(templatePath);

            var tableBuilder = new StringBuilder();
            var rowTemplate = _config.RowTemplate;

            foreach (var appointment in appointments)
            {
                var row = rowTemplate.Replace("{LocationName}", WebUtility.HtmlEncode(appointment.LocationName))
                    .Replace("{AppointmentDate}", appointment.AppointmentDate.ToShortDateString());

                tableBuilder.AppendLine(row);
            }

            return template.Replace("{rows}", tableBuilder.ToString());
        }
    }
}
