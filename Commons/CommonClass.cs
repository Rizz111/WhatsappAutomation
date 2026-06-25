using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsappAutomation.Commons;

public static class CommonClass
{

    public static string ReadSetting(string key)
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

}
