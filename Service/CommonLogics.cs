using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsappAutomation.Services
{
    internal static class CommonLogics
    {

        public static Company_Master Company_Master;

    }

 
    public class Company_Master 
    { 
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Comp_Code { get; set; }

        public string Comp_Name { get; set; }
        public string CompShortName { get; set; }

        public string Add1 { get; set; }
        public string Add2 { get; set; }
        public string Add3 { get; set; }
        public string Add4 { get; set; }
        public int PinCode { get; set; }
        public string Fact_Add1 { get; set; }
        public string Fact_Add2 { get; set; }
        public string Fact_City { get; set; }
        public int? Fact_CityCode { get; set; }
        public int? Fact_PinCode { get; set; }
        public string Tele1 { get; set; }
        public string Tele2 { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }
        public string Web { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string Password { get; set; }
        public int? Level { get; set; }
        public DateTime? Books_Beg { get; set; }
        public string LstNo { get; set; }
        public string CstNo { get; set; }
        public string Pan { get; set; }
        public string Subject { get; set; }
        public string Dir { get; set; }
        public string Logo1 { get; set; }
        public string Logo2 { get; set; }
        public decimal? IntRate { get; set; }
        public string Cond1 { get; set; }
        public string Cond2 { get; set; }
        public string Cond3 { get; set; }
        public string Cond4 { get; set; }
        public string Cond5 { get; set; }
        public string Cond6 { get; set; }
        public string AdharNumber { get; set; }
        public string UANNumber { get; set; }
        public string ExRange { get; set; }
        public string ExcRate { get; set; }
        public string ExDivision { get; set; }
        public string ExeccNo { get; set; }
        public string GSTNo { get; set; }
        public DateTime? GSTApplyDate { get; set; }
        public bool Einvoice { get; set; }
        public bool EWayBill { get; set; }
        public string APIUserName { get; set; }
        public string APIPassword { get; set; }
        public string APITestPassword { get; set; }
        public string AuthToken { get; set; }
        public DateTime? AuthDate { get; set; }

        public bool WhatApp { get; set; }
        public bool SMS { get; set; }
        public string SMSUserName { get; set; }
        public string SMSPassword { get; set; }
        public string CinNumber { get; set; }
        public string StateName { get; set; }
        public string StateType { get; set; }
        public string StateGSTCode { get; set; }
        public int? StateCode { get; set; }

        public string CityName { get; set; }
        public int? CityCode { get; set; }
        public string CompType { get; set; }
        public string TANNo { get; set; }
        public string TANDetail { get; set; }
        public string BankName { get; set; }
        public int? BankCode { get; set; }
        public string BankBranch { get; set; }
        public string BankIFSCCode { get; set; }
        public string BankAcNo { get; set; }
        public DateTime? ExeUpdate { get; set; }
        public string ExeVersion { get; set; }
        public string ExeDataType { get; set; }
        public string TCSYn { get; set; }
        public decimal TCSPer { get; set; }
        public DateTime? TCSDate { get; set; }
        public decimal TCSLimit { get; set; }
        public decimal TCSNoPAN { get; set; }
        public string TCSPost { get; set; }
        public decimal TCSRound { get; set; }
        public DateTime? TCSAmendDate { get; set; }

        public string SMTPServer { get; set; }
        public string SMTPPort { get; set; }
        public bool SMTPSSL { get; set; }
        public string SenderEmail { get; set; }
        public string MailPassword { get; set; }

        //public DateTime? GSTRLockDate { get; set; }
        //public DateTime? GSTRBlockDate { get; set; }

        public string ImagePath { get; set; }
        public byte[] Logo { get; set; }

        public string SoftwareVersion { get; set; }

        public int? OurPartyCode { get; set; }

        public bool IsOfficialWhatsApp { get; set; }
        public string WABAUserId { get; set; }
        public string WABAUserPassword { get; set; }
        public string PhoneNoId { get; set; }
        public string WABAID { get; set; }
        public string WABAToken { get; set; }
        public DateTime? WABAAuthTokenCallTime { get; set; }
    }

}