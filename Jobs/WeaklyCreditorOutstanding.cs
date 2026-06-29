using DevExpress.DirectX.Common.DirectWrite;
using Microsoft.Data.SqlClient;
using Quartz;
using System.Reflection.Emit;
using TexERP.Commons;
using WhatsappAutomation.Commons;
using WhatsappAutomation.DataContext;
using WhatsappAutomation.Service;
using WhatsappAutomation.Services;

namespace WhatsappAutomation.Jobs;

public class WeaklyCreditorOutstanding : IJob
{
    private WhatsappService _service;
    private ReportServiceDeskTop _reportservice;
    private readonly GetSqlData sqldata;
    private EmailService _emailService;

    public WeaklyCreditorOutstanding(WhatsappService service, ReportServiceDeskTop reportService, GetSqlData sql, EmailService email)
    {
        _service = service;
        _reportservice = reportService;
        sqldata = sql;
        _emailService = email;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        await RunWeeklyCreditorOutstanding();
    }


    private async Task RunWeeklyCreditorOutstanding()
    {
        try
        {
            var Filterstring = @$"[LateDays] > 30";

            var pdfBytes = await _reportservice.GenerateReportAsync("OutsCredBill", "OutStanding", Filterstring);
            var compnayinfo = await sqldata.GetListAsync<Compnay>($@"Select PhoneNoId, WABAUserId, WABAUserPassword, WABAID, WABAToken, WABAAuthTokenCallTime, Comp_Name, Mobile, GSTNo from Company_Master");


            var company = compnayinfo.FirstOrDefault();
            string FileName = $@"DailyCredOuts{DateTime.Today:ddMMMyyyy}.pdf";
            var fileResponse =  await _service.UploadFile(pdfBytes, FileName, company.PhoneNoId,company.WABAToken);//for upload file single time

            string OwnerMobileNumber = CommonClass.ReadSetting("QuartzJobs:WeeklyCredOutstanding:MobileNo");
            string OwnerEmailAddress = CommonClass.ReadSetting("QuartzJobs:WeeklyCredOutstanding:Email");
            var mobileNumbers = OwnerMobileNumber.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();
            var EmailAddress = OwnerEmailAddress.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();
            bool useEmail = CommonClass.ReadSetting1<bool>("NotificationSettings:UseEmail");
            bool useWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseWhatsApp");
            if (useWhatsApp)
            {
                foreach (var mobile in mobileNumbers)//for send file same file to multiple mobile numbers
                {
                    await _service.SendDocument(company.PhoneNoId, company.WABAToken, mobile, fileResponse.id, FileName, "document", company.Comp_Name);

                }
            }

            if (useEmail)
            {
                foreach (var email in EmailAddress)//for send file same file to multiple email address
                {
                    await _emailService.SendEmail("Weekly Creditor Outstanding Report", "Please find the attached report.", pdfBytes, FileName, DateTime.Now, email, company.Comp_Name, OwnerEmailAddress, company.SMTPSSL);
                }
            }


            Console.WriteLine($"All Weekly Creditor Outstanding Reports Sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }


    public class DailyDebOutsClass
    {
        public string PartyName { get; set; }
        public string BillNos { get; set; }
        public string MobileNo { get; set; }
        public string Emails { get; set; }

        public decimal Balance { get; set; }
        public decimal OutStanding { get; set; }
        public int MaxLateDays { get; set; }
        public int PartyCode { get; set; }
        public int? AgentCode { get; set; }
    }
}