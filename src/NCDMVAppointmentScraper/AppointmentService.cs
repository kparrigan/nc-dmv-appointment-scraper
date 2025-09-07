using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal class AppointmentService(ChromeDriver driver, ILogger<AppointmentService> logger, ScraperConfig config) : IAppointmentService
    {
        private readonly ILogger<AppointmentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ScraperConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly ChromeDriver _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        private WebDriverWait _wait;

        public List<AppointmentRecord> GetAppointments()
        {
            try
            {
                const string locationsDivSelector = ".step-control-content.UnitIdList.QFlowObjectModel.UnitDataControl";
                var appointments = new List<AppointmentRecord>();

                _logger.LogInformation("Checking for locations with open appointments.");

                _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_config.SeleniumWaitTimeSeconds));
                ProcessWelcomePage();
                ProcessAppointmentTypesPage();

                //Wait for the locations div to load
                _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(locationsDivSelector)));

                var processedLocations = new HashSet<string>();
                var location = GetNextLocation(processedLocations);

                // Iterate through all locations that appear to have available appointments
                while (location != null)
                {
                    const string loadingSelector = ".blockUI.blockOverlay";
                    var appointmentDate = GetNextAppointment(location.Value.Element); //Get next appointment if there is one.

                    if (appointmentDate.HasValue)
                    {
                        appointments.Add(new AppointmentRecord(location.Value.Name, appointmentDate.Value));
                    }

                    // Wait until the loading spinner appears and then disappears
                    _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(loadingSelector)));
                    _wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector(loadingSelector)));

                    // Hit the back button to return to the location list
                    var backButton = _driver.FindElement(By.Id("BackButton"));
                    backButton.Click();

                    // Find the next location if one is available
                    _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(locationsDivSelector)));
                    location = GetNextLocation(processedLocations);
                }

                return appointments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointments.");
                throw;
            }
            finally
            {
                _driver.Close();
            }
        }

        private void ProcessWelcomePage()
        {
            _driver.Navigate().GoToUrl(_config.DmvUrl);

            var apptButton = _driver.FindElement(By.Id("cmdMakeAppt")); //click 'Make Appointment' Button on initial page.
            apptButton.Click();
        }
        private void ProcessAppointmentTypesPage()
        {
            const string renewSelector = "div.QflowObjectItem[data-id='3']";
            _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(renewSelector)));
            var renewDiv = _driver.FindElement(By.CssSelector(renewSelector));
            renewDiv.Click(); //TODO: still getting intermittent problems after this click
        }

        private DateTime? GetNextAppointment(IWebElement locationElement)
        {
            // Click the loxation's div and wait for the calendar to load
            locationElement.Click();
            _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".CalendarDateModel.hasDatepicker")));

            // Sometimes a location will appear to have available appointments, but when clicked will show a message saying there aren't any ¯\_(ツ)_/¯
            var validationError = _driver.FindElement(By.CssSelector(".field-validation-error"));
            if (validationError.Displayed)
            {
                return null;
            }

            // Get the first appointment, if any, and return it's date.
            var appts = _driver.FindElements(By.CssSelector(".ui-state-default.ui-state-active"));
            if (appts != null && appts.Any())
            {
                var appt = appts[0];
                var parent = appt.FindElement(By.XPath(".."));

                if (!int.TryParse(appt.Text, out var day))
                {
                    return null;
                }

                if (!int.TryParse(parent.GetAttribute("data-month"), out var month))
                {
                    return null;
                }

                if (!int.TryParse(parent.GetAttribute("data-year"), out var year))
                {
                    return null;
                }

                month++;

                return new DateTime(year, month, day);
            }

            return null;
        }

        private (string Name, IWebElement Element)? GetNextLocation(HashSet<string> processed)
        {
            // Find the first location that hasn't been processed yet and return it
            var locations = _driver.FindElements(By.CssSelector(".QflowObjectItem.form-control.ui-selectable.Active-Unit.valid"));
            foreach (var location in locations)
            {
                var locationNameDiv = location.FindElement(By.XPath(".//div/div[1]"));
                var locationName = locationNameDiv.Text;

                if (!processed.Contains(locationName))
                {
                    processed.Add(locationName);
                    return (locationName, location);
                }
            }

            return null;
        }
    }
}
