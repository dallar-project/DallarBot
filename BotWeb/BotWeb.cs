using System;

namespace BotWeb
{
    public class BotWebServer : Nancy.NancyModule
    {
        public BotWebServer()
        {
            Get["/"] = _ => "Hello World!";
        }
        
    }
}
