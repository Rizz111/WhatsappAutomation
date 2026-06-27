using DevExpress.XtraRichEdit.Layout.Engine;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsappAutomation.DataContext;
using WhatsappAutomation.Service;
using static WhatsappAutomation.SendMediaPayLoad;

namespace WhatsappAutomation.Jobs;

public class GenrateToken : IJob
{
    private readonly GetSqlData sqldata;
    private MainDataContext _db;

    public GenrateToken(GetSqlData sqldata, MainDataContext db)
    {
        this.sqldata = sqldata;
        _db = db;
    }



    public async Task Execute(IJobExecutionContext context)
    {
        await GenrateTokenJob();
    }


    public async Task GenrateTokenJob()
    {
       var company = await sqldata.GetCompanyInfo();

        var options = new RestClientOptions("https://waba.texinfotech.com")
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
        var client = new RestClient(options);
        var request = new RestRequest("/AuthTokenV1/AuthToken", Method.Post);
       
        request.AddHeader("Content-Type", "application/json");
        var body = new AuthTokenPayLoad
        {
            UserId = company.WABAUserId,
            Password = company.WABAUserPassword,
        };

        string newText = JsonConvert.SerializeObject(body);

        request.AddParameter("application/json", newText, ParameterType.RequestBody);
        RestResponse response = await client.ExecuteAsync(request);
        
        //Console.WriteLine(response.Content);
        AuthTokenResponce authTokenResponce = JsonConvert.DeserializeObject<AuthTokenResponce>(response.Content);
        if (authTokenResponce != null)
        {
            if (authTokenResponce.IsSuccess)
            {
                //AppLogger.Info($"Auth token obtained successfully | TxnOutcome={authTokenResponce.TxnOutcome}");\
                string Query = $@"update Company_Master set WABAToken='{authTokenResponce.TxnOutcome}',WABAAuthTokenCallTime='{DateTime.Now:dd-MMM-yyyy HH:mm:ss}'";
              await  _db.Database.ExecuteSqlRawAsync(Query);
            }
            else
            {
                
            }
        }
    }


}
