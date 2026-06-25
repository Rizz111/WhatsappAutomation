using DevExpress.XtraRichEdit.Model;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;
using RestSharp;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Authentication;
using System.Threading.Tasks;
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
    public EmailService(MainDataContext db, GetSqlData getSql)
    {
        _db = db;
        GetSqlData = getSql;
    }

    private static void WriteLog(string message)
    {
        try
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Email");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string logFile = Path.Combine(logDir, $"EmailLog_{DateTime.Now:yyyy-MM-dd}.log");
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

            lock (_logLock)
            {
                File.AppendAllText(logFile, line);
            }
        }
        catch
        {
            // Swallow logging errors to avoid impacting email flow
        }
    }


    public async Task SendEmail(string Title, string BodyMgs, byte[] fileBytes, string fileName, DateTime dateTime, string recipientEmail, string Comp_Name, string OwnerEmailAddress)
    {
        try
        {
            var compnay = await GetSqlData.GetCompanyInfo();
            var message = new MimeMessage();
            //message.From.Add(new MailboxAddress(smtpUsername, smtpUsername));
            message.From.Add(new MailboxAddress(compnay.SenderEmail, compnay.MailPassword));
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

            var OwnerEmailAddressLs = OwnerEmailAddress.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();

            if (OwnerEmailAddressLs.Count() > 1)
            {
                for (int i = 1; i < OwnerEmailAddressLs.Count(); i++)
                {
                    message.Bcc.Add(new MailboxAddress(OwnerEmailAddressLs[i], OwnerEmailAddressLs[i]));
                }
            }

            BodyBuilder builder = new()
            {
                HtmlBody = @$"{BodyMgs}
<br> <br> <br>
<b>Sent from {Comp_Name} at {dateTime:dd-MMM-yyyy HH-mm-ss}
<br>We Run on Tex ERP 12.0</b>"
            };

            //if (FilePath != string.Empty && File.Exists(FilePath))
            //{
            //    builder.Attachments.Add(FilePath);
            //}

            if (fileBytes == null || fileBytes.Length == 0)
            {
                builder.Attachments.Add(
                    fileName ?? "Attachment",
                    fileBytes);
            }

            message.Body = builder.ToMessageBody();
            SendEmailWithRetry(message, compnay.SMTPServer, compnay.SMTPPort, compnay.SenderEmail, compnay.MailPassword, true);
        }
        catch (Exception ex)
        {
        }
    }

    private void SendEmailWithRetry(MimeMessage message, string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, bool ssl)
    {
        int retryCount = 0;
        int delayMs = InitialDelayMs;
        Exception lastException = null;

        while (retryCount < MaxRetryAttempts)
        {
            try
            {
                using var client = new MailKit.Net.Smtp.SmtpClient();
                try
                {
                    // Primary attempt with configured SSL option
                    client.Connect(smtpServer, smtpPort, ssl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);
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
                return; // Success
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex) when (ex.Message.Contains("AUTH005"))
            {
                lastException = ex;
                retryCount++;

                if (retryCount < MaxRetryAttempts)
                {
                    WriteLog($"AUTH005 error - Rate limited. Retry {retryCount}/{MaxRetryAttempts} in {delayMs}ms");
                    Thread.Sleep(delayMs);
                    delayMs *= 2; // Exponential backoff
                }
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex)
            {
                // Other SMTP protocol errors - don't retry
                WriteLog($"SMTP protocol error: {ex.Message}");
                throw;
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                // Authentication failed - credentials are invalid, don't retry
                WriteLog($"Authentication failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Network or other errors - may be worth retrying
                WriteLog($"Email send error: {ex.Message}");
                lastException = ex;
                retryCount++;

                if (retryCount < MaxRetryAttempts)
                {
                    WriteLog($"Retrying in {delayMs}ms");
                    Thread.Sleep(delayMs);
                    delayMs *= 2;
                }
            }
        }

        // All retries exhausted
        if (lastException != null)
        {
            WriteLog($"Email send failed after {MaxRetryAttempts} attempts: {lastException.Message}");
        }
    }
}