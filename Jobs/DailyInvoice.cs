using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Layout.Engine;
using Quartz;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    private readonly ILogger<Worker> _logger;


    public DailyInvoice(GetSqlData sql, ReportServiceDeskTop report, WhatsappService service, EmailService emailService, ILogger<Worker> logger)
    {
        sqldata = sql;
        _report = report;
        _service = service;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await GenrateInvoce();
    }

    public async Task GenrateInvoce()
    {
        int FabCount = 0, GFabCount = 0, JobCount = 0, YarnCount = 0;

        bool UseOfficialWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseOfficialWhatsApp");
        var AdminMobile = CommonClass.ReadSetting("QuartzJobs:DailyInvoice:MobileNo");
        bool useEmail = CommonClass.ReadSetting1<bool>("NotificationSettings:UseEmail");
        bool useWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseWhatsApp");
        var SendType = CommonClass.ReadSetting("QuartzJobs:DailyInvoice:SendType");

        var BccEmail = CommonClass.ReadSetting("QuartzJobs:DailyInvoice:BccEmail");
        var AdminEmail = CommonClass.ReadSetting("QuartzJobs:DailyInvoice:AdminEmail");

        var Company = await sqldata.GetCompanyInfo();
        string Condition = "";
        string JobRunDate = "";




        if (SendType == "Prev Day")
        {
            JobRunDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)).ToString("dd-MMM-yyyy");
            Condition = "format(bm.AckDate, 'dd-MMM-yyyy') = format(DateAdd(d, -1, GetDate()), 'dd-MMM-yyyy')";
            //Condition = "format(bm.AckDate, 'dd-MMM-yyyy') = '21-FEB-2026'";
        }
        else
        {
            JobRunDate = DateOnly.FromDateTime(DateTime.Today).ToString("dd-MMM-yyyy");
            Condition = "format(bm.AckDate, 'dd-MMM-yyyy') = format(GetDate(), 'dd-MMM-yyyy')";
        }

        string ReportName = "";
        var invoicedata = await sqldata.GetListAsync<InvoiceClass>($@"SELECT ac.ac_Name as PartyName, bm.Party_Code, agent.Ac_Name AgentName, STRING_AGG('''' + CAST(bm.FVNo AS VARCHAR(50)) + '''', ', ') AS FVNo, String_Agg(bm.Bill_No, ', ') Bill_Nos,
Max(ac.Mobile + ', ' + IsNull(agent.Mobile, '') + ', ' + IsNull(bt.BrandMobile, '')) as Mobile,
Max(ac.Email + ', ' + IsNull(agent.Email, '') + ', ' + IsNull(bt.BrandEmail, '')) as Email,
Max(Book_Type) Book_Type
from Bill_Master bm 
Inner join book_Master BB on BB.Book_Code = bm.Book_Code
INNER JOIN Account_Master ac ON ac.Ac_Code = bm.party_Code
LEFT JOIN Account_Master agent ON ac.Agent_Code = agent.Ac_Code
Left join (select FVNo, Max(brm.Email) BrandEmail, Max(brm.MobileNo) BrandMobile from Bill_Trans btt
            left join Brand_Master brm on btt.BrandCode = brm.BrandCode
            group by FVNo) as bt  on bm.FVNo = bt.FVNo
where {Condition} and Left(BB.DrCr, 1) = 'D' and BB.Book_Type in ('FABRIC', 'GFABRIC','JOB', 'YARNFA')
Group by Party_Code, ac.ac_Name, agent.Ac_Name, bb.Book_Code");

        foreach (var invoice in invoicedata)
        {

            if (invoice.Book_Type == "FABRIC")
            {
                ReportName = "InvoiceGSTeinvWA";
                FabCount++;
            }
            else if (invoice.Book_Type == "GFABRIC")
            {
                ReportName = "GreyInvoiceGSTeinvWA";
                GFabCount++;
            }
            else if (invoice.Book_Type == "JOB")
            {
                ReportName = "JobInvoiceGSTeinvWA";
                JobCount++;
            }
            else if (invoice.Book_Type == "YARNFA")
            {
                ReportName = "YarnInvoiceGstEinvWA";
                YarnCount++;
            }

            Dictionary<string, string> Filterstring = new Dictionary<string, string>() { { "Bill_Trans", $@"[FVNo] In ({invoice.Fvno})" } };

            var Pdfbyte = await _report.GenerateReportAsync(ReportName, "Invoice", Filterstring, "Title", "Range");//upload document to whatsapp single time or genreate there id 

            string FileName = $@"Invoice {invoice.PartyName}_{DateTime.Now:yyyyMMdd}.Pdf";


            if (useWhatsApp)
            {
                _logger.LogInformation($"WhatsApp Service Hit");
                var mobileNumbers = invoice.Mobile
                             .Split(',')
                             .Select(x => x.Trim()).Distinct().Where(z => z != "")
                             .ToList();
                if (UseOfficialWhatsApp)//for official whatsapp send document to multiple mobile numbers
                {
                    var fileresponse = await _service.UploadFile(Pdfbyte, FileName, Company.PhoneNoId, Company.WABAToken);

                    foreach (var mobile in mobileNumbers)//send same file to multiple mobile numbers after this loop id genrate for new document
                    {
                        await _service.SendDocument(Company.PhoneNoId, Company.WABAToken, mobile, fileresponse.id, FileName, "document", Company.Comp_Name);
                    }
                }
                else
                {
                    //////for unofficial whatsapp send document to multiple mobile numbers
                    ///

                    string folderPath = Path.Combine(AppContext.BaseDirectory, "ExportPdf", "INVOICE");

                    // Create folder if it doesn't exist
                    Directory.CreateDirectory(folderPath);


                    string filePath = Path.Combine(folderPath, FileName);

                    // Save PDF
                    await File.WriteAllBytesAsync(filePath, Pdfbyte);

                    var response = _service.GetDetails();
                    if (response == true)
                    {
                        foreach (var mobile in mobileNumbers)//send same file to multiple mobile numbers after this loop id genrate for new document
                        {
                            _service.SendReq(@$"Dear Customer, We Are Sending You Invoice {invoice.Bill_Nos} from *{Company.Comp_Name}*", mobile, filePath);
                        }

                        //==========================summary of sent invoice=========================
                    }
                    else
                    {
                        Console.WriteLine("Your Port Is Not Open Plese Check And Open Them For Use WhatsappService");
                    }
                }
            }

            if (useEmail)
            {
                _logger.LogInformation($"Email Service Hit");
                await _emailService.SendEmail(@$"{invoice.PartyName}-Invoice from {Company.Comp_Name}", $"Dear {invoice.PartyName}, Please find attached invoice for your reference.", Pdfbyte, $@"Invoice{invoice.PartyName}_{DateTime.Now:yyyyMMdd}", DateTime.Now, invoice.Email, BccEmail, Company.SMTPSSL);
            }
        }

        Console.WriteLine("Invoice Job Done");
        //===========================For Confirmation To Admin And Company===========================
        if (invoicedata.Count() != 0)
        {
            string Text = $"Dear Admin {JobRunDate}Total Invoice Sent : {invoicedata.Count()} , Fabric : {FabCount} , Grey Fabric : {GFabCount} , Job : {JobCount} , Yarn : {YarnCount}";

            if (useWhatsApp)
            {
                if (UseOfficialWhatsApp)//for official whatsapp send document to multiple mobile numbers
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Total Invoice Sent: {invoicedata.Count()}");

                    if (FabCount != 0)
                        sb.Append($", Fabric: {FabCount}");

                    if (GFabCount != 0)
                        sb.Append($", Grey Fabric: {GFabCount}");

                    if (JobCount != 0)
                        sb.Append($", Job: {JobCount}");

                    if (YarnCount != 0)
                        sb.Append($", Yarn: {YarnCount}");

                    string summary = sb.ToString();

                    await _service.SendTextWithTemplateMessage(Company.PhoneNoId, Company.WABAToken, AdminMobile, "adminconfirmation", summary);//For Admin Confirmation

                    await _service.SendTextWithTemplateMessage(Company.PhoneNoId, Company.WABAToken, "8233029994", "adminconfirmation", summary);//For Company Confirmation
                }
                else
                {
                    _service.SendReq(Text, AdminMobile, "");//For Admin Confirmation
                    _service.SendReq(Text, "8233029994", "");//For Company Confirmation
                }
            }

            if (useEmail)
            {
                await _emailService.SendEmail(@$"Daily Invoice Summary For {JobRunDate}", Text, null, null, DateTime.Now, AdminEmail, "", Company.SMTPSSL);
                await _emailService.SendEmail(@$"Daily Invoice Summary For {JobRunDate} {Company.Comp_Name}", Text, null, null, DateTime.Now, "asohil07@yahoo.com", "", Company.SMTPSSL);
            }
        }
    }
}