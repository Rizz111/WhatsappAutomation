using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Layout.Engine;
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

namespace WhatsappAutomation.Jobs;

public class DailyInvoice : IJob
{
    private readonly GetSqlData sqldata;
    private ReportServiceDeskTop _report;
    private WhatsappService _service;
    private EmailService _emailService;



    public DailyInvoice(GetSqlData sql,ReportServiceDeskTop report, WhatsappService service,EmailService emailService)
    {
        sqldata = sql;
        _report = report;
        _service = service;
         _emailService = emailService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      await   GenrateInvoce();
    }

    public async Task GenrateInvoce()
    {
        string ReportName = "";
        var invoicedata = await sqldata.GetListAsync<InvoiceClass>($@"SELECT ac.ac_Name as PartyName, bm.Party_Code, agent.Ac_Name AgentName, String_Agg(bm.FVNo, ', ') Fvno,
Max(ac.Mobile + ', ' + IsNull(agent.Mobile, '') + ', ' + IsNull(bt.BrandMobile, '')) as Mobile,
Max(ac.Email + ', ' + IsNull(agent.Email, '') + ', ' + IsNull(bt.BrandEmail, '')) as Email,
Max(Book_Type) Book_Type
from Bill_Master bm 
inner join (select FVNo, Max(brm.Email) BrandEmail, Max(brm.MobileNo) BrandMobile from Bill_Trans btt
            left join Brand_Master brm on btt.BrandCode = brm.BrandCode
            group by FVNo) as bt  on bm.FVNo = bt.FVNo
Inner join book_Master BB on BB.Book_Code = bm.Book_Code
INNER JOIN Account_Master ac ON ac.Ac_Code = bm.party_Code
LEFT JOIN Account_Master agent ON ac.Agent_Code = agent.Ac_Code
where 
--format(bm.AckDate, 'dd-MMM-yyyy') = format(DateAdd(d, -1, GetDate()), 'dd-MMM-yyyy')
format(bm.AckDate, 'dd-MMM-yyyy') = '21-May-2026'
and Left(BB.DrCr, 1) = 'D' and BB.Book_Type in ('FABRIC', 'GFABRIC','JOB', 'YARNFA')
Group by Party_Code, ac.ac_Name, agent.Ac_Name, bb.Book_Code");

        var invoices = invoicedata.FirstOrDefault();
        if (invoices.Book_Type == "FABRIC")
        {
            ReportName = "InvoiceGSTeinvWA";
        }
        else if (invoices.Book_Type == "GFABRIC")
        {
            ReportName = "GreyInvoiceGSTeinvWA";
        }
        else if (invoices.Book_Type == "JOB")
        {
            ReportName = "JobInvoiceGSTeinvWA";
        }
        else if (invoices.Book_Type == "YARNFA")
        {
            ReportName = "YarnInvoiceGstEinvWA";

        }
        var BccEmail = CommonClass.ReadSetting("QuartzJobs:DailyInvoice:Email");


        foreach (var invoice in invoicedata)
        {
            string Filterstring = $@"[Fvno]={invoice.Fvno}";
           var Pdfbyte= await _report.GenerateReportAsync(ReportName,"Invoice", Filterstring,"Title","Range");//upload document to whatsapp single time or genreate there id 
           var Company = await sqldata.GetCompanyInfo();


            bool useEmail = CommonClass.ReadSetting1<bool>("NotificationSettings:UseEmail");
            bool useWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseWhatsApp");
            if (useWhatsApp)
            {
                var fileresponse = await _service.UploadFile(Pdfbyte, $@"Invoice{invoice.PartyName}_{DateTime.Now:yyyyMMdd}", Company.PhoneNoId, Company.WABAToken);
                var mobileNumbers = invoice.Mobile
                         .Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(x => x.Trim()).Distinct()
                         .ToList();




                foreach (var mobile in mobileNumbers)//send same file to multiple mobile numbers after this loop id genrate for new document
                {

                    await _service.SendDocument(Company.PhoneNoId, Company.WABAToken, mobile, fileresponse.id, $@"Invoice{invoice.PartyName}_{DateTime.Now:yyyyMMdd}", "document", Company.Comp_Name);

                }
            }

            if (useEmail)
            {
                await _emailService.SendEmail("Invoice", $"Dear {invoice.PartyName}, Please find attached invoice for your reference.", Pdfbyte, $@"Invoice{invoice.PartyName}_{DateTime.Now:yyyyMMdd}", DateTime.Now, Company.Comp_Name, invoice.Email, BccEmail, Company.SMTPSSL);
            }


        }

    }

}