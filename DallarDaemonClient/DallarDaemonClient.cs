using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dallar
{
    public class DaemonClient
    {
        public Uri uri;
        public ICredentials credentials;

        public DaemonClient(string _uri)
        {
            var uriBuilder = new UriBuilder(_uri);
            uri = uriBuilder.Uri;
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

        public string CreateNewAddressForUser(string Account)
        {
            return InvokeMethod("getnewaddress", Account)["result"].ToString();
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

        public decimal GetRawAccountBalance(string Account)
        {
            return (decimal)InvokeMethod("getbalance", Account, 6)["result"];
        }

        public decimal GetUnconfirmedAccountBalance(string Account)
        {
            decimal pending = (decimal)InvokeMethod("getbalance", Account, 0)["result"];
            return pending - GetRawAccountBalance(Account);
        }

        public string GetAccountAddress(string Account)
        {
            return InvokeMethod("getaccountaddress", Account)["result"].ToString();
        }

        public string GetAccountAddressSubtle(string Account)
        {
            List<String> list = InvokeMethod("getaddressesbyaccount", Account)["result"].ToObject<List<String>>();
            return list[0];
        }

        public bool DoesAccountExist(string Account)
        {
            List<String> list = InvokeMethod("getaddressesbyaccount", Account)["result"].ToObject<List<String>>();
            return (list.Count >= 1);
        }

        public string SendToAddress(string fromAccount, string toWallet, decimal amount)
        {
            return InvokeMethod("sendfrom", fromAccount, toWallet, amount, 1, "")["result"].ToString();
        }

        public decimal GetTransactionFee(string txid)
        {
            return (decimal)InvokeMethod("gettransaction", txid)["result"]["fee"];
        }

        public bool MoveToAddress(string fromAccount, string toAccount, decimal amount)
        {
            string TransactionID = InvokeMethod("move", fromAccount, toAccount, amount, 1, "")["result"].ToString();
            return (TransactionID != null && TransactionID != "");
        }

        public bool SendMinusFees(string fromAccount, string toWallet, decimal amount, decimal Fee, string FeeAccount)
        {
            string txid = SendToAddress(fromAccount, toWallet, amount);
            if (txid != null || txid != "")
            {
                decimal transactionFee = GetTransactionFee(txid);
                decimal remainder = transactionFee + Fee;
                return MoveToAddress(fromAccount, FeeAccount, remainder);
            }
            return false;
        }

        public int GetBlockCount()
        {
            return (int)InvokeMethod("getblockcount")["result"];
        }

        public bool GetWalletAddressFromAccount(string Account, bool createIfNotFound, out string walletAddress)
        {
            walletAddress = "";
            if (DoesAccountExist(Account))
            {
                walletAddress = GetAccountAddressSubtle(Account);
                return true;
            }
            else
            {
                if (createIfNotFound)
                {
                    walletAddress = CreateNewAddressForUser(Account);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsAddressValid(string address)
        {
            if (address.Length > 20 && address.Length < 48)
            {
                if (!address.Contains("O") && !address.Contains("I") && !address.Contains("l") && !address.Contains("0"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
