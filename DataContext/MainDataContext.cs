using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace WhatsappAutomation.DataContext;

public class MainDataContext : DbContext
{
    private static string ReadSetting(string key)
    {
        try
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();//.AddEnvironmentVariables().Build();
            return config.GetSection(key).Value ?? "";
        }
        catch
        {
            Console.WriteLine("Error reading app settings");
            return string.Empty;
        }
    }

    public MainDataContext() : base()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        //optionsBuilder.UseSqlServer(MainConnectionString());
        optionsBuilder.UseSqlServer(MainConnectionString(), options =>
        {
            options.CommandTimeout(120); // Timeout in seconds
        });
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
    }

    public static string MainConnectionString()
    {
        //string ServerName = ReadSetting("ServerName");
        string ServerName ="Texserver";
        
        //string DbName = ReadSetting("DbName");
        string DbName ="TechRanjan2401";

        
        var SQLConn = new SqlConnectionStringBuilder()
        {
            UserID = "sa",
            Password = "scot$123",
            DataSource = ServerName,
            ApplicationName = "TexPayroll",
            InitialCatalog = DbName,
            ConnectTimeout = 180,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            MaxPoolSize = 500,
        };
        return SQLConn.ConnectionString;
    }

    //public DbSet<AppConfiguration> AppConfiguration { get; set; }
    
    


}
