using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexERP.Commons;
using WhatsappAutomation.Commons;
using WhatsappAutomation.Service;

namespace WhatsappAutomation.Jobs
{
    internal class DailyDebitorOutstanding:IJob
    {
        private readonly GetSqlData sqldata;
        private WhatsappService _service;
        private EmailService _emailService;
        public DailyDebitorOutstanding(GetSqlData sql, WhatsappService service,EmailService email)
        {
            sqldata = sql;
            _service = service;
            _emailService = email;
        }


        public async Task Execute(IJobExecutionContext context)
        {
            await DebJob.RunWeeklyDebOutsob(sqldata, _service, _emailService, RunTpye.Daily);
        }

    }
}
