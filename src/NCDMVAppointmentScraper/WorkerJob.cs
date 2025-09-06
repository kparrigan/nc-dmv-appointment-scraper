using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal class WorkerJob : IJob
    {
        private readonly Worker _worker;

        public WorkerJob(Worker worker)
        {
            _worker = worker;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _worker.Work();
        }
    }
}
