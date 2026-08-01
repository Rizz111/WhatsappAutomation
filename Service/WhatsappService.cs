using DevExpress.Utils.Svg;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Concurrent;
using System.IO;
using WhatsappAutomation.Commons;

namespace WhatsappAutomation.Service;

public class WhatsappService
{

    private static readonly object _logLock = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _phoneLocks =
    new ConcurrentDictionary<string, SemaphoreSlim>();




    public async Task<string> SendTextWithTemplateMessage(string PhoneNumberID, string Token, string MobileNo, string WaType, params string[] strings)
    {
#if DEBUG
        MobileNo = "7023160286";
#endif

        // Normalize the number first
        if (MobileNo.Length <= 10)
        {
            MobileNo = $"91{MobileNo}";
        }

        try
        {
            var client = new RestClient(
                     new RestClientOptions("https://messagingapi.charteredinfo.com")
                     {
                         Timeout = TimeSpan.FromSeconds(60)
                     });
            string url = $"/v19.0/{PhoneNumberID}/messages";


            var request = new RestRequest(@$"/v19.0/{PhoneNumberID}/messages", RestSharp.Method.Post);
            request.AddHeader("Authorization", @$"Bearer {Token}");

            var body = new SendMediaPayLoad
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                type = "template",
                to = @$"{MobileNo}",
            };

            body.template = new SendMediaPayLoad.Template
            {
                name = WaType,
                language = new SendMediaPayLoad.Language
                {
                    code = "en"
                },
            };

            if (body.template.components == null)
                body.template.components = new();

            var parameters = new List<SendMediaPayLoad.Parameter>();

            foreach (var x in strings)
            {
                parameters.Add(new SendMediaPayLoad.Parameter
                {
                    type = "text",
                    text = x,
                    Document = null
                });
            }

            body.template.components.Add(new SendMediaPayLoad.Component
            {
                type = "body",
                parameters = parameters
            });


            string newText = JsonConvert.SerializeObject(body);


            request.AddParameter("application/json", newText, ParameterType.RequestBody);

            RestResponse response = await client.ExecuteAsync(request);
            if (response != null)
                if (response.IsSuccessful && response.Content != null)
                {
                    var SendTextResp = response.Content;

                    SendWaRespTex? resppObj = JsonConvert.DeserializeObject<SendWaRespTex>(SendTextResp);
                    if (resppObj != null)
                    {
                        if (resppObj.contacts != null)
                        {
                            if (resppObj.contacts[0].wa_id == "")
                            {
                                WriteLog($"Given Number is not on WhatsApp: {MobileNo}");
                                Console.WriteLine($"Given Number is not on WhatsApp: {MobileNo}");

                                return "Given Number is not on WhatsApp";
                            }

                            WriteLog($"Message sent successfully to: {MobileNo}");
                            Console.WriteLine($"Message sent successfully to: {MobileNo}");
                            return "OK";
                        }
                    }
                }
                else
                {
                    WriteLog($"Failed to send message to: {MobileNo}. Status Code: {response.StatusCode}, Error Message: {response.ErrorMessage}");
                    Console.WriteLine($"Failed to send message to: {MobileNo}. Status Code: {response.StatusCode}, Error Message: {response.ErrorMessage}");
                }
        }
        finally
        {
            
        }
        return string.Empty;
    }

    public async Task<UploadMedia> UploadFile(byte[] fileBytes, string fileName, string PhoneNumberID, string Token)
    {
        if (fileBytes == null || fileBytes.Length == 0)
            return new UploadMedia();

        var client = new RestClient(
            new RestClientOptions("https://messagingapi.charteredinfo.com")
            {
                Timeout = TimeSpan.FromSeconds(60)
            });

        var request = new RestRequest(
            $"/v19.0/{PhoneNumberID}/media",
            Method.Post);

        request.AddHeader("Authorization", $"Bearer {Token}");

        request.AlwaysMultipartFormData = true;

        request.AddParameter("messaging_product", "whatsapp");

        request.AddFile(
            "file",
            fileBytes,
            fileName,
            "application/pdf");

        RestResponse response = await client.ExecuteAsync(request);

        WriteLog($"File upload Response for {fileName}: {response.Content}");
        Console.WriteLine($@"File upload Response for {fileName}: {response.Content}");

        if (!response.IsSuccessful)
        {
            Console.WriteLine(
                $"Error for file {fileName}: {response.StatusCode} - {response.ErrorMessage}");
            WriteLog($"Error for {fileName}: {response.StatusCode} - {response.ErrorMessage}");
            return new UploadMedia();
        }

        if (string.IsNullOrWhiteSpace(response.Content))
            return new UploadMedia();

        try
        {
            return JsonConvert.DeserializeObject<UploadMedia>(response.Content)
                   ?? new UploadMedia();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization Error: {ex.Message}");
            WriteLog($"Deserialization Error: {ex.Message}");
            return new UploadMedia();
        }
    }

    public async Task<string> SendDocument(string PhoneNumberID, string Token, string MobileNo, string DocumentId, string FileName, string WaType, params string[] strings)
    {
#if DEBUG
        MobileNo = "7023160286";
#endif

        var client = new RestClient(
                     new RestClientOptions("https://messagingapi.charteredinfo.com")
                     {
                         Timeout = TimeSpan.FromSeconds(60)
                     });
        string url = $"/v19.0/{PhoneNumberID}/messages";

        //var UplResp = await UploadMedia(fileBytes, FileName, PhoneNumberID, Token);
        //if (UplResp == "")
        //{

        //    return string.Empty;
        //}
        //UploadMedia UploadMediaResp = JsonConvert.DeserializeObject<UploadMedia>(UplResp);

        var request = new RestRequest(@$"/v19.0/{PhoneNumberID}/messages", RestSharp.Method.Post);
        request.AddHeader("Authorization", @$"Bearer {Token}");
        if (MobileNo.Length > 10)
        {
        }
        else
        {
            MobileNo = $@"91{MobileNo}";
        }
        var body = new SendMediaPayLoad
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            type = "template",
            to = @$"{MobileNo}",
        };

        body.template = new SendMediaPayLoad.Template
        {
            name = WaType,
            language = new SendMediaPayLoad.Language
            {
                code = "en"
            },
        };

        var HeaderCompoments = new SendMediaPayLoad.Component
        {
            type = "header",
            parameters = new List<SendMediaPayLoad.Parameter>
                    {
                        new SendMediaPayLoad.Parameter
                        {
                            type = "document",
                            Document = new SendMediaPayLoad.Document
                            {
                                id = DocumentId,
                                filename = FileName
                            }
                        }
                    }
        };

        body.template.components = new List<SendMediaPayLoad.Component> { HeaderCompoments };

        //foreach (var x in strings)
        //{
        //    body.template.components.Add(new SendMediaPayLoad.Component
        //    {
        //        type = "body",
        //        parameters = new List<SendMediaPayLoad.Parameter>
        //            {
        //                new SendMediaPayLoad.Parameter
        //                {
        //                    type = "text",
        //                    text = x
        //                }
        //            }
        //    });
        //}

        if (body.template.components == null)
            body.template.components = new();

        var parameters = new List<SendMediaPayLoad.Parameter>();

        foreach (var x in strings)
        {
            parameters.Add(new SendMediaPayLoad.Parameter
            {
                type = "text",
                text = x,
                Document = null
            });
        }

        body.template.components.Add(new SendMediaPayLoad.Component
        {
            type = "body",
            parameters = parameters
        });


        string newText = JsonConvert.SerializeObject(body);

        request.AddParameter("application/json", newText, ParameterType.RequestBody);
        //request.AddParameter("text/plain", body, ParameterType.RequestBody);
        RestResponse response = await client.ExecuteAsync(request);
        WriteLog($"Message Response Of {MobileNo} With {FileName}:- {response.Content}");
        Console.WriteLine($"Response For {FileName}: {response.Content}");
        if (response != null)
            if (response.IsSuccessful && response.Content != null)
            {
                var SendTextResp = response.Content;

                SendWaRespTex resppObj = JsonConvert.DeserializeObject<SendWaRespTex>(SendTextResp);
                if (resppObj != null)
                {
                    if (resppObj.contacts != null)
                    {
                        if (resppObj.contacts[0].wa_id == "")
                        {
                            return "Given Number is not on WhatsApp";
                            WriteLog($"Given Number is not on WhatsApp: {MobileNo}");
                            Console.WriteLine($"Given Number is not on WhatsApp: {MobileNo}");
                        }
                        return "OK";
                        WriteLog($"Message sent successfully to: {MobileNo} with  file: {FileName}");
                        Console.WriteLine($"Message sent successfully to: {MobileNo} with  file: {FileName}");
                    }
                }
                else
                {
                    WriteLog($"Failed to send message to: {MobileNo} with file: {FileName}. Response content is null.");
                    Console.WriteLine($"Failed to send message to: {MobileNo} with file: {FileName}. Response content is null.");
                }
            }
            else
            {
                WriteLog($"Failed to send message to: {MobileNo} with file: {FileName}. Status Code: {response.StatusCode}, Error Message: {response.ErrorMessage}");
                Console.WriteLine($"Failed to send message to: {MobileNo} with file: {FileName}. Status Code: {response.StatusCode}, Error Message: {response.ErrorMessage}");
            }
        return string.Empty;
    }
    private static void WriteLog(string message)
    {
        try
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Whatsapp");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string logFile = Path.Combine(logDir, $"WhatsappLog_{DateTime.Now:yyyy-MM-dd}.log");
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
    public class SendWaRespTex
    {
        public string messaging_product { get; set; }
        public List<Contact> contacts { get; set; }
        public List<Message> messages { get; set; }

        public class Contact
        {
            public string input { get; set; }
            public string wa_id { get; set; }
        }

        public class Message
        {
            public string id { get; set; }
        }
    }

    /*

    //////--------------------------------------------------------------------------------------------------UnOffical Whatsapp Send Document To Multiple Mobile Numbers
    ///

    public bool GetDetails()
    {
        var serverName = CommonClass.ReadSetting("ServerName");
        serverName = serverName
                             .Split(',')
                             .Select(x => x.Trim()).Distinct().Where(z => z != "")
                             .ToList()[0];
        var client = new RestClient(@$"http://{serverName}:3000/me");
        var request = new RestRequest()
        {
            Timeout = new TimeSpan(0, 0, 30),
            Method = RestSharp.Method.Get,
        };
        var response = client.Execute(request);
        if (response.IsSuccessful == true)
        {
            return true;

        }
        else
        {
            return false;
        }

        Console.WriteLine(response.IsSuccessful);


    }


    public string SendReq(string Message, string MobileNo, string PdfPath)
    {
        if (PdfPath != string.Empty)
            PdfPath = Path.GetFullPath(PdfPath);
#if DEBUG
        MobileNo = "7023160286";
#endif

        var serverName = CommonClass.ReadSetting("ServerName");
        serverName = serverName
                             .Split(',')
                             .Select(x => x.Trim()).Distinct().Where(z => z != "")
                             .ToList()[0];
        RestResponse response = new();

        var client = new RestClient(@$"http://{serverName}:3000/send");
        var request = new RestRequest()
        {
            Timeout = new TimeSpan(0, 0, 30),
            Method = Method.Post,
        };

        var body = new SendData()
        {
            contact = MobileNo,
            file = PdfPath,
            message = Message,
        };
        request.AddJsonBody(body);
        //var JSno = (new List<SendData>() { body }).ToList().ConvertToJSON();
        response = client.Execute(request);


        //Console.WriteLine(response.Content);
        return response.Content;
    }
    internal class SendData
    {
        public string contact { get; set; }
        public string message { get; set; }
        public string file { get; set; }
    }
    */


}
