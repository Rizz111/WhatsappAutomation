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
    private readonly ILogger<Worker> _logger;

    public WeaklyCreditorOutstanding(WhatsappService service, ReportServiceDeskTop reportService, GetSqlData sql, EmailService email, ILogger<Worker> logger)
    {
        _service = service;
        _reportservice = reportService;
        sqldata = sql;
        _emailService = email;
        _logger = logger;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        await RunWeeklyCreditorOutstanding();
    }


    private async Task RunWeeklyCreditorOutstanding()
    {
        try
        {
            bool useEmail = CommonClass.ReadSetting1<bool>("NotificationSettings:UseEmail");
            bool useWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseWhatsApp");
            bool UseOfficialWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseOfficialWhatsApp");

            var Filterstring = @$"[LateDays] > 30";
            var compnayinfo = await sqldata.GetListAsync<Compnay>($@"Select PhoneNoId, WABAUserId, WABAUserPassword, WABAID, WABAToken, WABAAuthTokenCallTime, Comp_Name, Mobile, GSTNo from Company_Master");
            var company = compnayinfo.FirstOrDefault();
            _logger.LogInformation($@"Report CreditorOutStanding Pdf Genration Start At {DateTime.Now}");
            var pdfBytes = await _reportservice.GenerateReportAsync_Old("OutsCreditors", "OutStanding", Filterstring);
            _logger.LogInformation($@"Report CreditorOutStanding Pdf Genration Compleate At {DateTime.Now}");

            //---------------------------------------------------------------------------------------------------Report Pdf Generation-------

            string FileName = $@"DailyCredOuts{DateTime.Today:ddMMMyyyy}.pdf";
            _logger.LogInformation($@"Report Going For File Upload On Meta At {DateTime.Now}");
            var fileResponse = await _service.UploadFile(pdfBytes, FileName, company.PhoneNoId, company.WABAToken);//for upload file single time
            _logger.LogInformation($@"Report Upload Complete On Meta At {DateTime.Now}");

            //---------------------------------------------------------------------------------------------------Upload File On Meta Complete---------------------------------------------------------------------------------------------------

            string OwnerMobileNumber = CommonClass.ReadSetting("QuartzJobs:WeeklyCredOutstanding:MobileNo");
            string OwnerEmailAddress = CommonClass.ReadSetting("QuartzJobs:WeeklyCredOutstanding:Email");
            var mobileNumbers = OwnerMobileNumber.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();
            var EmailAddress = OwnerEmailAddress.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();

            //---------------------------------------------------------------------------------------------------Get Mobile Numbers and Email Addresses From Appsetting and Create A list If Multiple Number-------------------------------------

            if (useWhatsApp)
            {
                _logger.LogInformation($@"Whatsapp Job Start For Creditor OutStanding");
                if (UseOfficialWhatsApp)
                {

                    foreach (var mobile in mobileNumbers)//for send file same file to multiple mobile numbers
                    {
                       

                        await _service.SendDocument(company.PhoneNoId, company.WABAToken, mobile, fileResponse.id, FileName, "document", company.Comp_Name);

                    }

                }
                else 
                {
                    //------------------------------------Unofficial WhatsApp Service--------------------------------------

                    string folderPath = Path.Combine(AppContext.BaseDirectory, "ExportPdf", "CREDOUTS");

                    // Create folder if it doesn't exist
                    Directory.CreateDirectory(folderPath);


                    string filePath = Path.Combine(folderPath, FileName);

                    // Save PDF
                    await File.WriteAllBytesAsync(filePath, pdfBytes);

                    var response = _service.GetDetails();
                    if (response == true)
                    {
                        foreach (var mobile in mobileNumbers)//send same file to multiple mobile numbers after this loop id genrate for new document
                        {

                            var WhatsappNo = mobile;
#if DEBUG

                            WhatsappNo = "7023160286";
#endif
                            _service.SendReq(@$"Dear Customer, We Are Sending You Creditor Outstanding Report For *{company.Comp_Name}*", mobile, filePath);
                        }


                        //==========================summary of sent invoice=========================
                    }
                    else
                    {
                        Console.WriteLine("Your Port Is Not Open Plese Check And Open Them For Use WhatsappService");
                    }

                }
                _logger.LogInformation($@"Whatsapp Job Complete For Creditor OutStanding");

            }

            if (useEmail)
            {
                foreach (var email in EmailAddress)//for send file same file to multiple email address
                {
                    await _emailService.SendEmail("Weekly Creditor Outstanding Report", "Please find the attached report.", pdfBytes, FileName, DateTime.Now, email, OwnerEmailAddress, company.SMTPSSL);
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