using Microsoft.Extensions.Logging;
using NLog.Config;
using OpenQA.Selenium.BiDi.Communication;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NCDMVAppointmentScraper
{
    internal class WorkerJob(ILogger<WorkerJob> logger, Worker worker) : IJob
    {
        private readonly ILogger<WorkerJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly Worker _worker = worker ?? throw new ArgumentNullException(nameof(worker));


        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = Guid.NewGuid().ToString();
            NLog.MappedDiagnosticsLogicalContext.Set("jobId", jobId);

            _logger.LogInformation("Running worker job.");
            await _worker.Work();
        }
    }
}
