using System;
using System.Linq;

namespace WhatsappAutomation;

public class UploadMedia
{
    public string id { get; set; }
    public DateTime expires { get; set; }
}

public class SendMediaPayLoad
{
    public string messaging_product { get; set; }
    public string recipient_type { get; set; }
    public string to { get; set; }
    public string type { get; set; }
    public Template template { get; set; }

    public class Document
    {
        public string id { get; set; }
        public string filename { get; set; }
    }
    public class Component
    {
        public string type { get; set; }
        public List<Parameter> parameters { get; set; }
    }

    public class Language
    {
        public string code { get; set; }
    }

    public class Template
    {
        public string name { get; set; }
        public Language language { get; set; }
        public List<Component> components { get; set; }
    }


    public class Parameter
    {
        public string type { get; set; }
        public Document Document { get; set; }
        public string text { get; set; }
    }




}
