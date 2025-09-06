using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal record AppointmentRecord
    {
        public AppointmentRecord(string locationName, DateTime appointmentDate)
        {
            LocationName = locationName ?? throw new ArgumentNullException(nameof(locationName));
            AppointmentDate = appointmentDate;
        }

        public string LocationName { get; init; }
        public DateTime AppointmentDate { get; init; }
    }
}
