using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal class WebDriverFactory : IWebDriverFactory
    {
        public IWebDriver Create() => new ChromeDriver();
    }
}
