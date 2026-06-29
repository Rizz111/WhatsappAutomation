using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexERP.Commons;
using WhatsappAutomation.Commons;
using WhatsappAutomation.Service;
using WhatsappAutomation.Services;

namespace WhatsappAutomation.Jobs
{
    internal class DailyDebitorOutstanding:IJob
    {
        private readonly GetSqlData sqldata;
        private WhatsappService _service;
        private EmailService _emailService;
        private ReportServiceDeskTop _reportServiceDeskTop;
        public DailyDebitorOutstanding(GetSqlData sql, WhatsappService service,EmailService email, ReportServiceDeskTop reportService)
        {
            sqldata = sql;
            _service = service;
            _emailService = email;
            _reportServiceDeskTop = reportService;
        }


        public async Task Execute(IJobExecutionContext context)
        {
            await DebJob.RunWeeklyDebOutsob(sqldata, _service, _emailService, _reportServiceDeskTop, RunTpye.Daily);
        }

    }
}
