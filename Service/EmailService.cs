using DevExpress.XtraRichEdit.Model;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using RestSharp;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Authentication;
using System.Threading.Tasks;
using WhatsappAutomation;
using WhatsappAutomation.Commons;
using WhatsappAutomation.DataContext;
using WhatsappAutomation.Service;

namespace TexERP.Commons;

public class EmailService
{
    private const int MaxRetryAttempts = 2;
    private const int InitialDelayMs = 1000;

    private static readonly object _logLock = new();

    private MainDataContext _db;
    private GetSqlData GetSqlData;
    private readonly ILogger<Worker> _logger;
    public EmailService(MainDataContext db, GetSqlData getSql, ILogger<Worker> logger)
    {
        _db = db;
        GetSqlData = getSql;
        _logger = logger;
    }

    private static void WriteLog(string message,string FolderName)
    {
        try
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", FolderName);
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string logFile = Path.Combine(logDir, $"EmailLog_{DateTime.Now:yyyy-MM-dd}.log");
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

            lock (_logLock)
            {
                File.AppendAllText(logFile, line);
            }
        }
        catch (Exception ex)
        {
            
            throw; // optional, if you want the Quartz job to fail
        }
    }


    public async Task SendEmail(string Title, string BodyMgs, byte[] fileBytes, string fileName, DateTime dateTime, string recipientEmail, string OwnerEmailAddress, bool ssl)
    {
//#if DEBUG
//        recipientEmail = "rizzcoding@gmail.com";
//        OwnerEmailAddress = recipientEmail;
//#endif
        try
        {
            _logger.LogInformation($"Preparing to send email to {recipientEmail} with title '{Title}'");
            var compnay = await GetSqlData.GetCompanyInfo();
            var message = new MimeMessage();
            //message.From.Add(new MailboxAddress(smtpUsername, smtpUsername));
            message.From.Add(new MailboxAddress(compnay.SenderEmail, compnay.SenderEmail));
  recipientEmail = recipientEmail.TrimEnd(' ', ',');
            var recipientEmails = recipientEmail.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim()).Distinct()
                     .ToList();
            if (!recipientEmails.Any())
                return;

            bool brk = false;
            {
                foreach (var item in recipientEmails)
                {
                    if (!new EmailAddressAttribute().IsValid(item))
                    {
                        brk = true;
                        break;
                    }
                }
            }
            if (brk)
                return;

            message.Subject = Title;
            message.To.Add(new MailboxAddress(recipientEmails[0], recipientEmails[0]));

            if (recipientEmails.Count() > 1)
            {
                for (int i = 1; i < recipientEmails.Count(); i++)
                {
                    message.Cc.Add(new MailboxAddress(recipientEmails[i], recipientEmails[i]));
                }
            }

            if (OwnerEmailAddress.Any())
            {



                var OwnerEmailAddressLs = OwnerEmailAddress.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();

                if (OwnerEmailAddressLs.Count() > 1)
                {
                    for (int i = 1; i < OwnerEmailAddressLs.Count(); i++)
                    {
                        message.Bcc.Add(new MailboxAddress(OwnerEmailAddressLs[i], OwnerEmailAddressLs[i]));
                    }
                }
            }

            BodyBuilder builder = new()
            {
                HtmlBody = @$"{BodyMgs}
<br> <br> <br>
<b>Sent from {compnay.Comp_Name} at {dateTime:dd-MMM-yyyy HH-mm-ss}
<br>We Run on Tex ERP 12.0</b>"
            };

            //if (FilePath != string.Empty && File.Exists(FilePath))
            //{
            //    builder.Attachments.Add(FilePath);
            //}

            if (fileBytes != null)
            {
                builder.Attachments.Add(
                    (fileName ?? "Attachment") + ".pdf",
                    fileBytes);
            }

            message.Body = builder.ToMessageBody();
          await  SendEmailWithRetry(message, compnay.SMTPServer, Convert.ToInt32(compnay.SMTPPort), compnay.SenderEmail, compnay.MailPassword, ssl);
            _logger.LogInformation($"Retry Method CALL FOR EMAIL SEND");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while sending email to {recipientEmail} with title '{Title}'");
        }
    }

    private async Task SendEmailWithRetry(MimeMessage message, string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, bool ssl)
    {
        int retryCount = 0;
        int delayMs = InitialDelayMs;
        Exception lastException = null;

        while (retryCount < MaxRetryAttempts)
        {
            try
            {_logger.LogInformation($"Attempting to send email to {string.Join(", ", message.To.Select(x => x.ToString()))}. Attempt {retryCount + 1}/{MaxRetryAttempts}");
                using var client = new MailKit.Net.Smtp.SmtpClient();
                try
                {
                    // Primary attempt with configured SSL option
                    //client.Connect(smtpServer, smtpPort, ssl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);
                    SecureSocketOptions options;

                    if (smtpPort == 587)
                        options = SecureSocketOptions.StartTls;
                    else if (smtpPort == 465)
                        options = SecureSocketOptions.SslOnConnect;
                    else
                        options = SecureSocketOptions.Auto;

                    client.Connect(smtpServer, smtpPort, options);
                    _logger.LogInformation($"Connected to SMTP server {smtpServer}:{smtpPort} with SSL option {options}");  
                }
                catch (SslHandshakeException)
                {
                    // If primary SSL option fails, disconnect and try alternative
                    client.Disconnect(false);
                    client.Connect(smtpServer, smtpPort, ssl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                }

                client.Authenticate(smtpUsername, smtpPassword);
                _ = client.Send(message);
                client.Disconnect(true);
                _logger.LogInformation($"Email sent successfully to {string.Join(", ", message.To.Select(x => x.ToString()))}");
                WriteLog($"Email sent successfully to {string.Join(", ", message.To.Select(x => x.ToString()))}", "Email");
                return; // Success
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex) when (ex.Message.Contains("AUTH005"))
            {
                lastException = ex;
                retryCount++;

                if (retryCount < MaxRetryAttempts)
                {
                    WriteLog($"AUTH005 error - Rate limited. Retry {retryCount}/{MaxRetryAttempts} in {delayMs}ms", "Email");
                    _logger.LogError($"AUTH005 error - Rate limited. Retry {retryCount}/{MaxRetryAttempts} in {delayMs}ms");
                    Thread.Sleep(delayMs);
                    delayMs *= 2; // Exponential backoff
                }
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex)
            {
                // Other SMTP protocol errors - don't retry
                WriteLog($"SMTP protocol error: {ex.Message}", "Email");
                _logger.LogError($"SMTP protocol error: {ex.Message}");
                throw;
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                // Authentication failed - credentials are invalid, don't retry
                _logger.LogError($"Authentication failed: {ex.Message}");
                WriteLog($"Authentication failed: {ex.Message}", "Email");
                throw;
            }
            catch (Exception ex)
            {
                // Network or other errors - may be worth retrying
                _logger.LogError($"Email send error: {ex.Message}");
                WriteLog($"Email send error: {ex.Message}", "Email");
                lastException = ex;
                retryCount++;

                if (retryCount < MaxRetryAttempts)
                {_logger.LogInformation($"Retrying in {delayMs}ms");
                    WriteLog($"Retrying in {delayMs}ms", "Email");
                    Thread.Sleep(delayMs);
                    delayMs *= 2;
                }
            }
        }

        // All retries exhausted
        if (lastException != null)
        {_logger.LogError($"Email send failed after {MaxRetryAttempts} attempts: {lastException.Message}");
            WriteLog($"Email send failed after {MaxRetryAttempts} attempts: {lastException.Message}", "Email");
        }
    }
}