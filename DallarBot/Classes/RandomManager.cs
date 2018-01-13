using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DallarBot.Classes
{
    public class RandomJSON
    {
        public RandomResult result { get; set; }
    }

    public class RandomResult
    {
        public RandomData random { get; set; }
    }

    public class RandomData
    {
        public int BitsUsed { get; set; }
        public List<int> data { get; set; }
    }


    public class RandomManager
    {
        public int result = -1;

        public RandomManager(Int64 min, Int64 max)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.random.org/json-rpc/1/invoke");
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            JObject param = new JObject();
            param["apiKey"] = "fbef3728-6fa6-41d3-bd3e-0df830160a2e";
            param["n"] = 1;
            param["min"] = min;
            param["max"] = max;

            JObject sendObject = new JObject();
            sendObject["jsonrpc"] = "2.0";
            sendObject["id"] = "1";
            sendObject["method"] = "generateIntegers";
            sendObject.Add(new JProperty("params", param));

            string serializedObject = JsonConvert.SerializeObject(sendObject);

            byte[] byteArray = Encoding.UTF8.GetBytes(serializedObject);
            webRequest.ContentLength = byteArray.Length;

            using (Stream dataStream = webRequest.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream str = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        string rte = sr.ReadToEnd();
                        RandomJSON data = JsonConvert.DeserializeObject<RandomJSON>(rte);
                        result = data.result.random.data[0];
                    }
                }
            }
        }
    }
}
