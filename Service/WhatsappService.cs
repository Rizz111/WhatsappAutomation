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

            Token = $@"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJIb3N0SWQiOiIxMzkiLCJDbGllbnRJZCI6IjM0OTc1IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6Im1ldGEucmFuamFuZmFicmljc0BnbWFpbC5jb20iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJXYWJhIiwiZXhwIjoxNzgyMTUxNTUwLCJuYmYiOjE3ODIxMjk5NTB9.QIfwb-IojCo2sHnVyxKskiXcfPfKwzW3oNcZL3iwPks";
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

    public async Task<string> SendFile(string PhoneNumberID, string Token, string MobileNo, byte[] fileBytes, string FileName, string WaType, params string[] strings)
    {
        var client = new RestClient(
                     new RestClientOptions("https://messagingapi.charteredinfo.com")
                     {
                         Timeout = TimeSpan.FromSeconds(60)
                     });
        string url = $"/v19.0/{PhoneNumberID}/messages";

        var UplResp = await UploadMedia(fileBytes, FileName, PhoneNumberID,Token);
        if (UplResp == "")
        {
            
            return string.Empty;
        }
        UploadMedia UploadMediaResp = JsonConvert.DeserializeObject<UploadMedia>(UplResp);

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
                                id = UploadMediaResp.id,
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


    public async Task<string> UploadMedia(byte[] fileBytes,string fileName, string PhoneNumberID,string Token)
    {
        //if (File.Exists(FlePath))
        if (fileBytes.Length != 0)
        {
            var client = new RestClient(
                     new RestClientOptions("https://messagingapi.charteredinfo.com")
                     {
                         Timeout = TimeSpan.FromSeconds(60)
                     });
            string url = $"/v19.0/{PhoneNumberID}/messages";
           
            
            var request = new RestRequest(@$"/v19.0/{PhoneNumberID}/media", RestSharp.Method.Post);
            request.AddHeader("Authorization", $"Bearer {Token}");
            request.AlwaysMultipartFormData = true;
            request.AddParameter("messaging_product", "whatsapp");
            //request.AddFile("file", FilePath);
            //request.AddFile("file", FilePath, "application/pdf");
            request.AddFile( "file", fileBytes, fileName, "application/pdf");

            RestResponse response = await client.ExecuteAsync(request);
            Console.WriteLine(response.Content);
            if (response != null)
                if (response.IsSuccessful && response.Content != null)
                    return response.Content;
        }

        return string.Empty;
    }


    //public async Task<UploadMedia> UploadFile(byte[] fileBytes, string fileName, string PhoneNumberID, string Token)
    //{
    //    //if (File.Exists(FlePath))
    //    if (fileBytes.Length != 0)
    //    {
    //        var client = new RestClient(
    //                 new RestClientOptions("https://messagingapi.charteredinfo.com")
    //                 {
    //                     Timeout = TimeSpan.FromSeconds(60)
    //                 });
    //        string url = $"/v19.0/{PhoneNumberID}/messages";


    //        var request = new RestRequest(@$"/v19.0/{PhoneNumberID}/media", RestSharp.Method.Post);
    //        request.AddHeader("Authorization", $"Bearer {Token}");
    //        request.AlwaysMultipartFormData = true;
    //        request.AddParameter("messaging_product", "whatsapp");
    //        //request.AddFile("file", FilePath);
    //        //request.AddFile("file", FilePath, "application/pdf");
    //        request.AddFile("file", fileBytes, fileName, "application/pdf");

    //        RestResponse response = await client.ExecuteAsync(request);
    //        Console.WriteLine(response.Content);
    //        if (response.IsSuccessful &&
    //     !string.IsNullOrWhiteSpace(response.Content))
    //        {
    //            return JsonConvert.DeserializeObject<UploadMedia>(response.Content)
    //                   ?? new UploadMedia();
    //        }


    //    }

    //   return new UploadMedia();
    //}

    public async Task<UploadMedia> UploadFile(
    byte[] fileBytes,
    string fileName,
    string PhoneNumberID,
    string Token)
    {
        if (fileBytes == null || fileBytes.Length == 0)
            return new UploadMedia();
        Token = $@"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJIb3N0SWQiOiIxMzkiLCJDbGllbnRJZCI6IjM0OTc1IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6Im1ldGEucmFuamFuZmFicmljc0BnbWFpbC5jb20iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJXYWJhIiwiZXhwIjoxNzgyNDE0NDU3LCJuYmYiOjE3ODIzNzEyNTd9.22TqnemOiLsblhtrZvo_VCgFcqX20aBplTG84yG-yyU";
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


    public async Task<string> SendDocument(string PhoneNumberID, string Token, string MobileNo,string DocumentId, string FileName, string WaType, params string[] strings)
    {
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
        Token = $@"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJIb3N0SWQiOiIxMzkiLCJDbGllbnRJZCI6IjM0OTc1IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6Im1ldGEucmFuamFuZmFicmljc0BnbWFpbC5jb20iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJXYWJhIiwiZXhwIjoxNzgyNDE0NDU3LCJuYmYiOjE3ODIzNzEyNTd9.22TqnemOiLsblhtrZvo_VCgFcqX20aBplTG84yG-yyU";
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
