using DevExpress.Utils.Svg;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Concurrent;
using System.IO;

namespace WhatsappAutomation.Service;

public class WhatsappService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _phoneLocks =
    new ConcurrentDictionary<string, SemaphoreSlim>();

    public async Task<string> SendTextWithTemplateMessage(string PhoneNumberID, string Token, string MobileNo, string WaType, params string[] strings)
    {
#if DEBUG
        MobileNo = "8233029994";
#endif

        // Normalize the number first
        if (MobileNo.Length <= 10)
        {
            MobileNo = $"91{MobileNo}";
        }

        var semaphore = _phoneLocks.GetOrAdd(
            MobileNo,
            _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
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
                                return "Given Number is not on WhatsApp";
                            }
                            return "OK";
                        }
                    }
                }
        }
        finally
        {
            semaphore.Release();
            if (semaphore.CurrentCount == 1)
            {
                _phoneLocks.TryRemove(MobileNo, out _);
                semaphore.Dispose();
            }
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

        Console.WriteLine(response.Content);

        if (!response.IsSuccessful)
        {
            Console.WriteLine(
                $"Error: {response.StatusCode} - {response.ErrorMessage}");

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

            return new UploadMedia();
        }
    }

    public async Task<string> SendDocument(string PhoneNumberID, string Token, string MobileNo, string DocumentId, string FileName, string WaType, params string[] strings)
    {
#if DEBUG
        MobileNo = "8233029994";
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

        foreach (var x in strings)
        {
            body.template.components.Add(new SendMediaPayLoad.Component
            {
                type = "body",
                parameters = new List<SendMediaPayLoad.Parameter>
                    {
                        new SendMediaPayLoad.Parameter
                        {
                            type = "text",
                            text = x
                        }
                    }
            });
        }

        string newText = JsonConvert.SerializeObject(body);

        request.AddParameter("application/json", newText, ParameterType.RequestBody);
        //request.AddParameter("text/plain", body, ParameterType.RequestBody);
        RestResponse response = await client.ExecuteAsync(request);

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
                        }
                        return "OK";
                    }
                }
            }

        return string.Empty;
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
}
