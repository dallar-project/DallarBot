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
    public class DallarConnectionManager
    {
        public Uri uri;
        public ICredentials credentials;

        public DallarConnectionManager(string _uri)
        {
            uri = new Uri(_uri);
        }

        public JObject InvokeMethod(string _method, params object[] _params)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Credentials = credentials;

            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            JObject sendObject = new JObject();
            sendObject["jsonrpc"] = "1.0";
            sendObject["id"] = "1";
            sendObject["method"] = _method;

            if (_params != null)
            {
                if (_params.Length > 0)
                {
                    JArray properties = new JArray();
                    foreach (var param in _params)
                    {
                        properties.Add(param);
                    }
                    sendObject.Add(new JProperty("params", properties));
                }
            }

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
                        return JsonConvert.DeserializeObject<JObject>(rte);
                    }
                }
            }
        }

        public void CreateNewAddressForUser()
        {
            string NewAddress = (string)InvokeMethod("getnewaddress")["result"];
        }

        public float GetDifficulty()
        {
            return (float)InvokeMethod("getdifficulty")["result"];
        }

        public float GetConnectionCount()
        {
            return (int)InvokeMethod("getconnectioncount")["result"];
        }

        public JObject GetInformation()
        {
            return InvokeMethod("getinfo")["result"] as JObject;
        }

        public JObject GetMiningInformation()
        {
            return InvokeMethod("getmininginfo")["result"] as JObject;
        }
    }
}
