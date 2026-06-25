using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsappAutomation.Commons;
using WhatsappAutomation.DataContext;

namespace WhatsappAutomation.Service;

public class GetSqlData
{
    MainDataContext _db;
    public GetSqlData(MainDataContext db)
    {
        _db = db;
    }

    public async Task<List<T>> GetListAsync<T>(string Query) where T : class
    {
        try
        {
            var result = await _db.GetListAsync<T>(Query);
            return result.ToList();
        }
        catch (Exception ex)
        {
            throw new Exception("Error in GetListAsync", new Exception("Please Check the following method and Query " + $@"GetList()=> {Query}", ex));
        }
    }

    public  async Task<Compnay?> GetCompanyInfo()
    {
        var data = await _db.GetListAsync<Compnay>($@"Select PhoneNoId,WABAUserId,WABAUserPassword,WABAID,WABAToken,WABAAuthTokenCallTime,Comp_Name,Mobile,GSTNo,SMTPPort,SMTPServer,SenderEmail,MailPassword,Email from Company_Master");

        return data.FirstOrDefault();

    }


}
