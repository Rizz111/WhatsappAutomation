using DevExpress.DataAccess.Sql;
using DevExpress.DataAccess.Wizard.Services;

using Microsoft.Data.SqlClient;
using WhatsappAutomation.DataContext;

namespace WhatsappAutomation.ReportControllers;

public class CustomConnectionProviderService : IConnectionProviderService
{


    public SqlDataConnection LoadConnection(string connectionName)
    {
        //SqlConnectionStringBuilder conn = new()
        //{
        //    CommandTimeout = 50000,
        //    ConnectTimeout = 50000,
        //    TrustServerCertificate = true,
        //    Password = CommonClass.SQLPassword,
        //    UserID = CommonClass.SQLUserName,
        //    InitialCatalog = CommonClass.DbName,
        //    DataSource = CommonClass.ServerIP,
        //    MultipleActiveResultSets = true
        //};

        //var Con = new SqlDataConnection()
        //{
        //    ConnectionString = conn.ConnectionString,
        //    //Name = ServerConnectionLogic.MainDataBaseName() + "_Connection",
        //};
        //return Con;
        return new SqlDataConnection()
        {
            ConnectionString = MainDataContext.MainConnectionString()
        };
    }

    public string LoadConnection()
    {
        //SqlConnectionStringBuilder conn = new()
        //{
        //    CommandTimeout = 50000,
        //    ConnectTimeout = 50000,
        //    TrustServerCertificate = true,
        //    Password = Commons.SQLPassword,
        //    UserID = CommonClass.SQLUserName,
        //    InitialCatalog = CommonClass.DbName,
        //    DataSource = CommonClass.ServerIP,
        //    MultipleActiveResultSets = true
        //};

        //return conn.ConnectionString;

        return MainDataContext.MainConnectionString();
    }
}