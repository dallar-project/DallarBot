using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DallarBot.Services
{
    public class RandomManagerService
    {
        public int result = -1;

        public List<float> RandomFloats;
        public int FloatIndex = 0;

        public RandomManagerService()
        {
            FetchRandomNumbers();
        }

        public void FetchRandomNumbers()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.random.org/json-rpc/1/invoke");
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            JObject param = new JObject();
            param["apiKey"] = "eb30e56e-de45-43f3-b39c-1799da7a7b3a";
            param["n"] = 10;
            param["decimalPlaces"] = 7;

            JObject sendObject = new JObject();
            sendObject["jsonrpc"] = "2.0";
            sendObject["id"] = "1";
            sendObject["method"] = "generateDecimalFractions";
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
                        dynamic data = JsonConvert.DeserializeObject(rte);
                        RandomFloats = data.result.random.data.ToObject<List<float>>();
                        FloatIndex = 0;
                    }
                }
            }
        }

        public int GetRandomInteger(int min, int max)
        {
            if (FloatIndex >= RandomFloats.Count)
            {
                FetchRandomNumbers();
            }
            var RandomFloat = (int)Math.Round((float)min + (RandomFloats[FloatIndex]) * (float)max - (float)min);
            FloatIndex++;

            return RandomFloat;
        }
    }
}
