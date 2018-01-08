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
    public class ConnectionManager
    {
        public Uri uri;
        public ICredentials credentials;

        public ConnectionManager(string _uri)
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

        public string CreateNewAddressForUser(string accountName)
        {
            return InvokeMethod("getnewaddress", accountName)["result"].ToString();
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

        public decimal GetRawAccountBalance(string account)
        {
            return (decimal)InvokeMethod("getbalance", account, 6)["result"];
        }

        public decimal GetUnconfirmedAccountBalance(string account)
        {
            decimal pending = (decimal)InvokeMethod("getbalance", account, 0)["result"];
            return pending - GetRawAccountBalance(account);
        }

        public string GetAccountAddress(string account)
        {
            return InvokeMethod("getaccountaddress", account)["result"].ToString();
        }

        public string GetAccountAddressSubtle(string account)
        {
            List<String> list = InvokeMethod("getaddressesbyaccount", account)["result"].ToObject<List<String>>();
            return list[0];
        }

        public bool DoesAccountExist(string account)
        {
            List<String> list = InvokeMethod("getaddressesbyaccount", account)["result"].ToObject<List<String>>();
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

        public bool SendMinusFees(string fromAccount, string toWallet, string feeAccount, decimal txfee, decimal amount)
        {
            string txid = SendToAddress(fromAccount, toWallet, amount);
            bool success = (txid != null || txid != "");
            if (success)
            {
                decimal transactionFee = GetTransactionFee(txid);
                decimal remainder = transactionFee + txfee;
                success = MoveToAddress(fromAccount, feeAccount, remainder);
                return success;
            }
            return false;
        }

        public int GetBlockCount()
        {
            return (int)InvokeMethod("getblockcount")["result"];
        }

        public bool GetWalletAddressFromUser(string userID, bool createIfNotFound, out string walletAddress)
        {
            walletAddress = "";
            if (DoesAccountExist(userID))
            {
                walletAddress = GetAccountAddressSubtle(userID);
                return true;
            }
            else
            {
                if (createIfNotFound)
                {
                    walletAddress = CreateNewAddressForUser(userID);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool isAddressValid(string address)
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
