using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal interface IWebDriverFactory
    {
        IWebDriver Create();
    }
}
