using DevExpress.XtraReports.Services;
using DevExpress.XtraReports.UI;
using DevExpress.DataAccess.Wizard.Services;
using System.ComponentModel.Design;

namespace WhatsappAutomation.ReportControllers;

public class ReportByNameService : IReportProvider
{
    
    public XtraReport GetReport(string reportName, ReportProviderContext context)
    {
        string BaseDirectory = AppContext.BaseDirectory;
        XtraReport report = new();
        var reportFolder = Path.Combine(BaseDirectory, "Reports");
        try
        {
            if (File.Exists(reportName))
            {
                byte[] reportBytes = File.ReadAllBytes(reportName);
                using (MemoryStream ms = new(reportBytes))
                    report = XtraReport.FromXmlStream(ms);
                (report as IServiceContainer).AddService(typeof(IConnectionProviderService), new CustomConnectionProviderService());

            }
            else
            {
                return null;
            }

            return report;
        }
        catch
        {

            report = default;
            throw;

        }
        finally
        {
            report = default;
        }
    }

   


}

