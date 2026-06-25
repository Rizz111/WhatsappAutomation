using Quartz;
using TexERP.Commons;
using WhatsappAutomation.Commons;
using WhatsappAutomation.Service;

namespace WhatsappAutomation.Jobs;

public class WeaklyDebitorOutsanding : IJob
{
    private readonly GetSqlData sqldata;
    private WhatsappService _service;
    private EmailService _emailService;
    public WeaklyDebitorOutsanding(GetSqlData sql, WhatsappService service, EmailService email)
    {
        sqldata = sql;
        _service = service;
        _emailService = email;  
    }


    public async Task Execute(IJobExecutionContext context)
    {
        await DebJob.RunWeeklyDebOutsob(sqldata, _service, _emailService, RunTpye.Weakly);
    }


}