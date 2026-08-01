using DevExpress.CodeParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexERP.Commons;
using WhatsappAutomation.DataContext;
using WhatsappAutomation.Service;
using WhatsappAutomation.Services;
using static WhatsappAutomation.Jobs.WeaklyDebitorOutsanding;

namespace WhatsappAutomation.Commons;


enum RunTpye
{
    Daily,
    Weakly
}

internal class DebJob
{



    public static async Task RunWeeklyDebOutsob(GetSqlData sqldata, WhatsappService _service, EmailService email, ReportServiceDeskTop _reportservice, RunTpye runType)
    {
        //and(Case when IsNull(agent.CreditDays, 0) > 0 then agent.CreditDays else IsNull(ac.CreditDays, 0) end) > 0
        string cond = @$"";
        string BccEmail = "";
        if (runType == RunTpye.Daily)
        {
            cond = $@" where lateDays > 120";
            BccEmail = CommonClass.ReadSetting("QuartzJobs:DailyDebOutstanding:Email");

        }
        else if (runType == RunTpye.Weakly)
        {
            cond = $@" where lateDays between 90 and 120";
            BccEmail = CommonClass.ReadSetting("QuartzJobs:WeeklyDebOutstanding:Email");
        }
        try
        {
            string query = $@"WITH Bills AS
(
    SELECT o.PartyName, ac.Ac_Code PartyCode, agent.Ac_Code AgentCode, o.Bill_No + format(LateDays, ' (0 Days) ') Bill_No, o.Balance, o.DebitAmt, o.CreditAmt, o.LateDays,
        ROW_NUMBER() OVER (PARTITION BY o.PartyName ORDER BY o.LateDays DESC, o.Bill_Date) AS RN,
        ac.Mobile + ',' + IsNull(agent.Mobile, '') as Mobile,
        ac.Email + ',' + IsNull(agent.Email, '') as Email
    FROM vDebtorsOutstanding o
    INNER JOIN Account_Master ac ON ac.Ac_Code = o.Ac_Code
    LEFT JOIN Account_Master agent ON ac.Agent_Code = agent.Ac_Code
{cond} 
)
SELECT
    PartyName,
    PartyCode, AgentCode,
    STRING_AGG(CONVERT(NVARCHAR(MAX), Bill_No), ', ') AS BillNos,
    SUM(Balance) AS Balance,
    SUM(DebitAmt) - SUM(CreditAmt) AS Outstanding,
    MAX(LateDays) AS MaxLateDays,
    Max(Mobile) MobileNo,
    Max(Email) Emails
FROM Bills
WHERE RN <= 5 
GROUP BY PartyName,PartyCode,AgentCode having SUM(Balance)>0
ORDER BY MAX(LateDays) DESC, PartyName";
            var compnayinfo = await sqldata.GetListAsync<Compnay>($@"Select PhoneNoId, WABAUserId, WABAUserPassword, WABAID, WABAToken, WABAAuthTokenCallTime, Comp_Name, Mobile, GSTNo from Company_Master");
            var DebOutsLs = await sqldata.GetListAsync<DailyDebOutsClass>(query);

            var company = compnayinfo.FirstOrDefault();
            //List<Task<string>> whatsappTasks = new();
            //List<Task> emailTasks = new();



            foreach (var item in DebOutsLs)
            {
                var mobileNumbers = item.MobileNo
                             .Split(',')
                             .Select(x => x.Trim()).Distinct().Where(z => z != "")
                             .ToList();
                var emails = item.Emails
                             .Split(',')
                             .Select(x => x.Trim()).Distinct().Where(z => z != "")
                             .ToList();

                bool useEmail = CommonClass.ReadSetting1<bool>("NotificationSettings:UseEmail");
                bool useWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseWhatsApp");
                bool UseOfficialWhatsApp = CommonClass.ReadSetting1<bool>("NotificationSettings:UseOfficialWhatsApp");
                if (useWhatsApp)
                {
                    if (runType == RunTpye.Weakly)// if weekly then send party + owner only text template message
                    {
                        foreach(var mobile in mobileNumbers)
                        {
                           
                            await _service.SendTextWithTemplateMessage(company.PhoneNoId, company.WABAToken, mobile, "tempoutstanding", item.PartyName, item.BillNos, item.OutStanding.ToString("0.00"), company.Comp_Name);
                        }


                        //----------------------------------------------------------------------------------------------------------------send to party according to loop number
                        string OwnerNumber = CommonClass.ReadSetting1<string>("QuartzJobs:WeeklyDebOutstanding:MobileNo");
                        var OwnerNumbers = OwnerNumber
                             .Split(',')
                             .Select(x => x.Trim()).Distinct().Where(z => z != "")
                             .ToList();
                        foreach (var Omobile in OwnerNumbers)
                        {
                            await _service.SendTextWithTemplateMessage(company.PhoneNoId, company.WABAToken, Omobile, "tempoutstanding", item.PartyName, item.BillNos, item.OutStanding.ToString("0.00"), company.Comp_Name);
                                                     
                        };

                        //----------------------------------------------------------------------------------------------------------------send to owner according to loop number
                    }

                    if (runType == RunTpye.Daily) ///if daily then send only to custmore with there document
                    {
                        var Filterstring = @$"[Ac_code] = {item.PartyCode} And [LateDays] > 120";

                        var pdfBytes = await _reportservice.GenerateReportAsync_Old("OutsDebtors", "OutStanding", Filterstring);

                        string FileName = $@"DailyDebOuts{DateTime.Today:ddMMMyyyy}.pdf";
                        var fileResponse = await _service.UploadFile(pdfBytes, FileName, company.PhoneNoId, company.WABAToken);//for upload file single time

                        foreach(var mobile in mobileNumbers)
                        {
                           
                        await _service.SendDocument(company.PhoneNoId, company.WABAToken, mobile, fileResponse.id, FileName, "tempoutstandingdoc", item.PartyName, item.BillNos, item.OutStanding.ToString("0.00"), company.Comp_Name);
                           
                        }


                    }


                }

                if (useEmail)
                {

                    foreach (var item1 in emails)
                    {
                        email.SendEmail("Outstanding Alert", $@"Dear {item.PartyName},<br/><br/>Your Outstanding Balance is Rs. {item.OutStanding:0.00}.<br/>Please find the details of your outstanding bills below:<br/><br/>Bill Nos: {item.BillNos}<br/><br/>Kindly make the payment at the earliest to avoid any inconvenience.<br/><br/>Thank you,<br/>{company.Comp_Name}", null, null, DateTime.Now, item1, BccEmail, company.SMTPSSL);
                    }
                }
            }

            //await Task.WhenAll(whatsappTasks.Concat(emailTasks));

            Console.WriteLine($"Total Daily Invoice Class: {DebOutsLs.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
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


public class Compnay
{
    public string PhoneNoId { get; set; }
    public string WABAUserId { get; set; }
    public string WABAUserPassword { get; set; }
    public string WABAID { get; set; }
    public string WABAToken { get; set; }
    public string WABAAuthTokenCallTime { get; set; }
    public string Comp_Name { get; set; }
    public string Mobile { get; set; }
    public string GSTNo { get; set; }
    public int SMTPPort { get; set; }
    public string SMTPServer { get; set; }

    public string SenderEmail { get; set; }
    public string MailPassword { get; set; }
    public string Email { get; set; }
    public bool SMTPSSL { get; set; }

}


public class InvoiceClass
{
    public string PartyName { get; set; }
    public int Party_Code { get; set; }
    public string AgentName { get; set; }
    public string Fvno { get; set; }
    public string Mobile { get; set; }
    public string Email { get; set; }
    public string Bill_Nos { get; set; }
    public string Book_Type { get; set; }

}
