using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal class EmailConfig
    {
        public string ApiKey { get; set; }
        public string Sender { get; set; }
        public List<string> RecipientList { get; set; }
        public string Subject { get; set; }
        public string RowTemplate { get; set; }
        public string TemplateFile { get; set; }

        public static EmailConfig LoadFromConfiguration(IConfiguration configuration)
        {
            var config = new EmailConfig();
            configuration.GetSection("EmailConfig").Bind(config);
            return config;
        }
    }
}
