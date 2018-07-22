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
        protected Uri uri;
        protected ICredentials credentials;
        public decimal txFee;
        public DallarAccount FeeAccount;

        public DaemonClient(string _uri, string _username, string password, decimal txFee, DallarAccount FeeAccount)
        {
            credentials = new NetworkCredential(_username, password);
            var uriBuilder = new UriBuilder(_uri);
            uri = uriBuilder.Uri;
            this.txFee = txFee;
            this.FeeAccount = FeeAccount;
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

        public decimal GetRawAccountBalance(DallarAccount Account)
        {
            return (decimal)InvokeMethod("getbalance", Account.UniqueAccountName, 6)["result"];
        }

        public decimal GetUnconfirmedAccountBalance(DallarAccount Account)
        {
            decimal pending = (decimal)InvokeMethod("getbalance", Account.UniqueAccountName, 0)["result"];
            return pending - GetRawAccountBalance(Account);
        }

        public bool CanAccountAffordTransaction(DallarAccount Account, decimal Amount)
        {
            decimal balance = GetRawAccountBalance(Account);

            if (Amount + txFee > balance)
            {
                return false;
            }

            return true;
        }

        //public string GetAccountAddress(string Account)
        //{
        //    return InvokeMethod("getaccountaddress", accountPrefix + Account)["result"].ToString();
        //}

        public string GetAccountAddressSubtle(DallarAccount Account)
        {
            List<String> list = InvokeMethod("getaddressesbyaccount", Account.UniqueAccountName)["result"].ToObject<List<String>>();
            return list[0];
        }

        public bool DoesAccountExist(DallarAccount Account)
        {
            List<String> list = InvokeMethod("getaddressesbyaccount", Account.UniqueAccountName)["result"].ToObject<List<String>>();
            return (list.Count >= 1);
        }

        public string SendToAddress(DallarAccount FromAccount, DallarAccount ToAccount, decimal amount, bool bCreateToAccountIfNotFound)
        {
            if (!ToAccount.IsAddressKnown)
            {
                if (!string.IsNullOrEmpty(ToAccount.UniqueAccountName))
                {
                    if (!GetWalletAddressFromAccount(bCreateToAccountIfNotFound, ref ToAccount))
                    {
                        return null;
                    }
                }
            }

            return InvokeMethod("sendfrom", FromAccount.UniqueAccountName, ToAccount.KnownAddress, amount, 1, "")["result"].ToString();
        }

        public decimal GetTransactionFee(string txid)
        {
            return (decimal)InvokeMethod("gettransaction", txid)["result"]["fee"];
        }

        public bool MoveToAddress(DallarAccount FromAccount, DallarAccount ToAccount, decimal Amount)
        {
            string TransactionID = InvokeMethod("move", FromAccount.UniqueAccountName, FromAccount.UniqueAccountName, Amount, 1, "")["result"].ToString();
            return (TransactionID != null && TransactionID != "");
        }

        public bool SendMinusFees(DallarAccount FromAccount, DallarAccount ToAccount, decimal amount, bool bCreateToAccountIfNotFound)
        {
            string txid = SendToAddress(FromAccount, ToAccount, amount, bCreateToAccountIfNotFound);
            if (txid != null || txid != "")
            {
                decimal transactionFee = GetTransactionFee(txid);
                decimal remainder = transactionFee + txFee;
                return MoveToAddress(FromAccount, ToAccount, remainder);
            }
            return false;
        }

        public int GetBlockCount()
        {
            return (int)InvokeMethod("getblockcount")["result"];
        }

        public bool GetWalletAddressFromAccount(bool createIfNotFound, ref DallarAccount Account)
        {
            if (DoesAccountExist(Account))
            {
                Account.KnownAddress = GetAccountAddressSubtle(Account);
                return true;
            }
            else
            {
                if (createIfNotFound)
                {
                    Account.KnownAddress = InvokeMethod("getnewaddress", Account.UniqueAccountName)["result"].ToString();
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
