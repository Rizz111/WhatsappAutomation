using DevExpress.DataAccess.Sql;
using DevExpress.DataAccess.Wizard.Services;
using DevExpress.XtraCharts;
using DevExpress.XtraPrinting.Drawing;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using System.Drawing;
using WhatsappAutomation.DataContext;
using WhatsappAutomation.ReportControllers;

namespace WhatsappAutomation.Services
{
    public class ReportServiceDeskTop
    {
        private DevExpress.XtraReports.Services.IReportProvider serv;

        private MainDataContext _db;
        private readonly ILogger<Worker> _logger;
                
        public ReportServiceDeskTop(MainDataContext Db, DevExpress.XtraReports.Services.IReportProvider reportser, ILogger<Worker> logger)
        {
            _db = Db;
            serv = reportser;
            _logger = logger;
        }

        public async Task<byte[]> GenerateReportAsync(string reportName, string FolderName, Dictionary<string, string> SelectionFormula, string Title = "Report Title", string Range = "Report Range")
        {
            string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", FolderName, reportName + ".repx");

            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"Report file '{reportName}' not found.");
            }

            XtraReport Report = XtraReport.FromFile(reportPath);

            Report = serv.GetReport(reportPath, null);
            // new Research
            {
                if (SelectionFormula.Any())
                {
                    if (Report.DataSource is SqlDataSource SqlDS1)
                    {
                        foreach (var s in SqlDS1.Queries)
                        {
                            if ((s is SelectQuery))
                                (s as SelectQuery).FilterString = null;
                        }
                    }

                    if (Report.DataSource is SqlDataSource SqlDS)
                    {
                        List<string> MyFilterString = new();
                        foreach (var s in SelectionFormula)
                        {
                            var dm = SqlDS.Queries.Where(x => x.Name.ToUpper() == s.Key.ToUpper()).FirstOrDefault();
                            if (dm != null)
                            {
                                if (dm is SelectQuery XX)
                                {
                                    string Farm = s.Value;

                                    if (XX.FilterString != string.Empty)
                                    {
                                        if (!Farm.Trim().StartsWith("AND", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            Farm = " AND " + Farm;
                                        }
                                    }
                                    else
                                    {
                                        if (Farm.Trim().StartsWith("AND", StringComparison.InvariantCultureIgnoreCase) && Farm.Trim().Length > 4)
                                        {
                                            Farm = Farm.Substring(3, Farm.Length - 3);
                                        }
                                    }

                                    XX.FilterString = (XX.FilterString + Farm).Trim();

                                    if (XX.FilterString.Trim().StartsWith("AND", StringComparison.InvariantCultureIgnoreCase) && XX.FilterString.Trim().Length > 4)
                                    {
                                        XX.FilterString = XX.FilterString.Substring(3, XX.FilterString.Length - 3);
                                    }

                                    if (Report.DataMember == dm.Name)
                                    {
                                        MyFilterString.Add(XX.FilterString);
                                    }
                                    else
                                    {
                                        var dm1 = SqlDS.Relations.Where(x => x.MasterQueryName.ToUpper() == s.Key.ToUpper() || x.DetailQueryName.ToUpper() == s.Key.ToUpper()).FirstOrDefault();
                                        if (dm1 != null)
                                        {
                                            var Spl = XX.FilterString.Split(["AND", "and", "And"], StringSplitOptions.None);
                                            for (int itrator = 0; itrator < Spl.Length; itrator++)
                                            {
                                                var str = Spl[itrator];
                                                MyFilterString.Add("[" + dm1.Name + "]." + str);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Report.FilterString = string.Join(" AND ", MyFilterString);
                    }
                }
                SetDefaultValues(Report, Title, Range, new());
            }

            using (MemoryStream ms = new())
            {
                Report.Margins = new System.Drawing.Printing.Margins(20, 20, 20, 20); // Set margins programmatically
                Report.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
                try
                {
                    _logger.LogWarning("{reportName} MS Report Export Started at: {time}", reportName, DateTimeOffset.Now.ToString("dd-MMM-yyyy HH:mm:ss tt"));
                    Report.ExportToPdf(ms);
                    _logger.LogWarning("{reportName} MS Report Export Ended at: {time}", reportName, DateTimeOffset.Now.ToString("dd-MMM-yyyy HH:mm:ss tt"));
                }
                catch (Exception ex)
                {
                    throw;
                }

                return ms.ToArray();
            }
        }

        private XtraReport SetDefaultValues(XtraReport Report, string Title, string Range, Dictionary<string, string> DefText = null)
        {
            Report.ShowPreviewMarginLines = false;
            try
            {
                foreach (Band s in Report.Bands)
                {
                    SetTextFromCode(s, DefText, Title, Range);
                }
            }
            catch { }
            try
            {
                if (Report.DataSource is SqlDataSource SqlDS1)
                {
                    (Report as IServiceContainer).RemoveService(typeof(IConnectionProviderService));
                    (Report as IServiceContainer).AddService(typeof(IConnectionProviderService), new CustomConnectionProviderService());
                }
            }
            catch { }
            return Report;
        }

        private void SetTextFromCode(Band s, Dictionary<string, string> DefText,string Title, string Range)
        {
            foreach (XRControl item in s.Controls)
            {
                if (item is Band ss)
                    SetTextFromCode(ss, DefText, Title, Range);
                else
                    SetTextFromCode(item, DefText, Title, Range);
            }
            foreach (XRControl item in s.SubBands)
            {
                if (item is Band ss)
                    SetTextFromCode(ss, DefText, Title, Range);
                else
                    SetTextFromCode(item, DefText, Title, Range);
            }
        }

        private void SetTextFromCode(XRControl item, Dictionary<string, string> DefText, string Title, string Range)
        {
            if (item is XRSubreport subrpt)
            {
                if (subrpt.ReportSourceUrl != string.Empty)
                    try
                    {
                        (subrpt.Report as IServiceContainer).RemoveService(typeof(IConnectionProviderService));
                        (subrpt.Report as IServiceContainer).AddService(typeof(IConnectionProviderService), new CustomConnectionProviderService());
                    }
                    catch { }
            }

            if (DefText != null)
            {
                foreach (var itemx in DefText)
                {
                    if (itemx.Key.ToUpper() == item.Text.ToUpper())
                    {
                        item.Text = itemx.Value;
                    }
                }
            }

            if (item.Name.ToUpper().Equals("COMPANY".ToUpper()) || item.Text.ToUpper().Equals("COMPANY".ToUpper()) ||
        item.Name.ToUpper().Equals("companyname".ToUpper()) || item.Text.ToUpper().Equals("companyname".ToUpper()) ||
        item.Name.ToUpper().Equals("LbCompany".ToUpper()) || item.Text.ToUpper().Equals("LbCompany".ToUpper()) ||
        item.Name.ToUpper().Equals("companyname1".ToUpper()) || item.Text.ToUpper().Equals("companyname1".ToUpper()))
            {
                item.Text = CommonLogics.Company_Master.Comp_Name;
            }

            if (item.Name.ToUpper().EndsWith("TITLE") || item.Text.ToUpper().EndsWith("TITLE"))
            {
                item.Text = Title;
            }

            if (item.Name.ToUpper().EndsWith("ADDRESS") || item.Text.ToUpper().EndsWith("ADDRESS"))
            {
                item.Text = $@"{CommonLogics.Company_Master.Add1},{CommonLogics.Company_Master.Add2}, {CommonLogics.Company_Master.CityName}";
            }

            if (item.Name.ToUpper().EndsWith("ADD1") || item.Text.ToUpper().EndsWith("ADD 1"))
            {
                item.Text = $@"{CommonLogics.Company_Master.Add1}";
            }

            if (item is XRPictureBox Pp)
            {
                if (Pp.ExpressionBindings.Any())
                {
                    if (Pp.ExpressionBindings[0].Expression.Contains("CompLogo"))
                    {
                        using var ms = new MemoryStream(CommonLogics.Company_Master.Logo);
                        Pp.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(Image.FromStream(ms));
                    }
                }
            }

            if (item.Name.ToUpper().EndsWith("ADD2") || item.Text.ToUpper().EndsWith("ADD 2"))
            {
                item.Text = $@"{CommonLogics.Company_Master.Add2}";
            }

            if (item.Name.ToUpper().EndsWith("ADD3") || item.Text.ToUpper().EndsWith("ADD 3"))
            {
                item.Text = $@"{CommonLogics.Company_Master.Add3}";
            }

            if (item.Name.ToUpper().EndsWith("FACTADD1") || item.Text.ToUpper().EndsWith("FACTADD 1"))
            {
                item.Text = $@"{CommonLogics.Company_Master.Fact_Add1}";
            }

            if (item.Name.ToUpper().EndsWith("FACTADD2") || item.Text.ToUpper().EndsWith("FACTADD 2"))
            {
                item.Text = $@"{CommonLogics.Company_Master.Fact_Add2}";
            }

            if (item.Name.ToUpper().EndsWith("FACTADD") || item.Text.ToUpper().EndsWith("FACTADD"))
            {
                item.Text = $@"{CommonLogics.Company_Master.Fact_Add1}, {CommonLogics.Company_Master.Fact_Add2}, {CommonLogics.Company_Master.Fact_City}";
            }

            if (item.Name.ToUpper().EndsWith("RANGE") || item.Text.ToUpper().EndsWith("RANGE"))
            {
                item.Text = Range;
            }

            if (item.Text.ToUpper().StartsWith("WE RUN ON"))
            {
                item.Text = "We   Run   On   Tex   ERP   12.0";
            }
        }

        public async Task<byte[]> GenerateReportAsync_Old(string reportName, string FolderName, string Filterstring, string Title = "Report Title", string Range = "Report Range")
        {
            string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", FolderName, reportName + ".repx");

            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"Report file '{reportName}' not found.");
            }

            XtraReport Report = XtraReport.FromFile(reportPath);

            Report = serv.GetReport(reportPath, null);

            if (Filterstring != "")
            {
                string XX = Filterstring;
                XX = XX.Replace(Report.DataMember + ".", "");
                Report.FilterString = XX;
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
                Report.Margins = new System.Drawing.Printing.Margins(20, 20, 20, 20); // Set margins programmatically
                Report.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
                try
                {
                    Report.ExportToPdf(ms);
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