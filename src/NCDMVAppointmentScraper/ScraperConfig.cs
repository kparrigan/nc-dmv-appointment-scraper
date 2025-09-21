using Microsoft.Extensions.Configuration;

namespace NCDMVAppointmentScraper
{
    internal class ScraperConfig
    {
        public int SeleniumWaitTimeSeconds { get; set; }
        public string DmvUrl { get; set; }

        public HashSet<string> LocationsToMonitor { get; set; } = new HashSet<string>();

        public static ScraperConfig LoadFromConfiguration(IConfiguration configuration)
        {
            var config = new ScraperConfig();
            configuration.GetSection("ScraperConfig").Bind(config);
            return config;
        }
    }
}