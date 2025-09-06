using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal interface IAppointmentService
    {
        public List<AppointmentRecord> GetAppointments();
    }
}
