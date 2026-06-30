using Quartz;
using TexERP.Commons;
using WhatsappAutomation.Commons;
using WhatsappAutomation.Service;
using WhatsappAutomation.Services;

namespace WhatsappAutomation.Jobs;

public class WeaklyDebitorOutsanding : IJob
{
    private readonly GetSqlData sqldata;
    private WhatsappService _service;
    private EmailService _emailService;
    private ReportServiceDeskTop _reportservice;
    public WeaklyDebitorOutsanding(GetSqlData sql, WhatsappService service, EmailService email, ReportServiceDeskTop reportservice)
    {
        sqldata = sql;
        _service = service;
        _emailService = email;
        _reportservice = reportservice;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        await DebJob.RunWeeklyDebOutsob(sqldata, _service, _emailService, _reportservice,RunTpye.Weakly);
    }


}