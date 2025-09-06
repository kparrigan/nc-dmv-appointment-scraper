using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace NCDMVAppointmentScraper
{
    internal class Worker
    {
        const string RootUrl = "https://skiptheline.ncdot.gov/Webapp/Appointment/Index/a7ade79b-996d-4971-8766-97feb75254de";
        const int WaitTimeSeconds = 10;
        ChromeDriver _driver;
        WebDriverWait _wait;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _driver = new ChromeDriver();
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(WaitTimeSeconds));
        }

        public async Task Work()
        {
            _logger.LogInformation("Checking for locations with open appointments.");

            try
            {
                const string locationsDivSelector = ".step-control-content.UnitIdList.QFlowObjectModel.UnitDataControl";
                const string renewSelector = "div.QflowObjectItem[data-id='3']";

                _driver.Navigate().GoToUrl(RootUrl);

                var apptButton = _driver.FindElement(By.Id("cmdMakeAppt")); //click 'Make Appointment' Button on initial page.
                apptButton.Click();

                _wait.Until(drv => drv.FindElement(By.CssSelector(renewSelector))); //click 'Renew' option once page loads
                var renewDiv = _driver.FindElement(By.CssSelector(renewSelector));
                renewDiv.Click(); //TODO: still getting intermittent problems after this click

                _wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(locationsDivSelector)));

                var processedLocations = new HashSet<string>();
                var locationElement = GetNextLocation(processedLocations);

                // Iterate through all locations that appear to have available appointments
                while (locationElement != null)
                {
                    const string loadingSelector = ".blockUI.blockOverlay";
                    var appointment = GetNextAppointment(locationElement); //Get next appointment if there is one.

                    // Wait until the loading spinner appears and then disappears
                    _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(loadingSelector)));
                    _wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector(loadingSelector)));

                    // Hit the back button to return to the location list
                    var backButton = _driver.FindElement(By.Id("BackButton"));
                    backButton.Click();

                    // Find the next location if one is available
                    _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(locationsDivSelector)));
                    locationElement = GetNextLocation(processedLocations);
                }

                _logger.LogInformation("Processing Complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding appointments.");
            }
            finally
            {
                _driver.Close();
                _driver.Quit();
            }
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

        private IWebElement? GetNextLocation(HashSet<string> processed)
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
                    return location;
                }
            }

            return null;
        }
    }
}
