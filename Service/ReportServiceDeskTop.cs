using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.UI;

using Microsoft.EntityFrameworkCore;
using WhatsappAutomation.DataContext;

namespace WhatsappAutomation.Services
{
    public class ReportServiceDeskTop
    {

        private DevExpress.XtraReports.Services.IReportProvider serv;

        MainDataContext _db;
        public ReportServiceDeskTop(MainDataContext Db, DevExpress.XtraReports.Services.IReportProvider reportser)
        {
            _db = Db;
            serv = reportser;

        }

        public async Task<byte[]> GenerateReportAsync(string reportName, string FolderName, string Filterstring, string Title = "Report Title", string Range = "Report Range")
        {

            
            string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", FolderName, reportName + ".repx");

            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"Report file '{reportName}' not found.");
            }

            XtraReport rpt = XtraReport.FromFile(reportPath);

            rpt = serv.GetReport(reportPath, null);

            if (Filterstring != "")
            {
                string XX = Filterstring;
                XX = XX.Replace(rpt.DataMember + ".", "");
                rpt.FilterString = XX;
            }

            //if (rpt.DataSource is SqlDataSource SQLDs)
            //{
            //    if (DataFilter != null)
            //    {
            //        foreach (var item in DataFilter)
            //        {
            //            var dm = SQLDs.Queries.FirstOrDefault(x => x.Name.ToUpper() == item.Key.ToUpper());
            //            if (dm != null)
            //            {
            //                if (dm is SelectQuery Qry)
            //                {
            //                    Qry.FilterString = item.Value;
            //                }
            //            }
            //        }
            //    }
            //}
            using (MemoryStream ms = new())
            {
                rpt.Margins = new System.Drawing.Printing.Margins(20, 20, 20, 20); // Set margins programmatically
                rpt.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
                try
                {
                    rpt.ExportToPdf(ms);
                 }
                catch (Exception ex)
                {

                    throw;
                }

                return ms.ToArray();
            }
        }

    }
}
