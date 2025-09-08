using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal interface IEmailService
    {
        public void SendEmail(List<AppointmentRecord> appointments);
    }
}
