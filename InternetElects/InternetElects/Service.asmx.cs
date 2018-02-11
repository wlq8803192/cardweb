using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Linq;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web.UI.MobileControls;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace InternetElects
{
    /// <summary>
    /// Summary description for Service
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Service : System.Web.Services.WebService
    {
        private Uri address;
        private String strPrivateKeyTraining = "Jh76krkj5JghdOnx9";
        private String strPrivateKeyProducing = "Jh76krkj5JghdOnx9";//Jh76krkj5JghdOnx9
        private String strPrivateKeyProducingEx = "Jh76krkj5JghdOnx9";//12hhS8as8styq2dejASDASDADsbga0616n
        private String strCancel = ConfigurationManager.AppSettings["strCancel"];
        private String strFreeze = ConfigurationManager.AppSettings["strFreeze"];
        private String strPostPone = ConfigurationManager.AppSettings["strPostPone"];
        private String strQueryOrder = ConfigurationManager.AppSettings["strQueryOrder"];
        private String strResendMsgInfo = ConfigurationManager.AppSettings["strResendMsgInfo"];
        private String strResendMailInfo = ConfigurationManager.AppSettings["strResendMailInfo"];
        private String strPlaceMobileOrder = ConfigurationManager.AppSettings["strPlaceMobileOrder"];
        private String strPlaceExcelOrder = ConfigurationManager.AppSettings["strPlaceExcelOrder"];
        private String strCtqyMultiCard = ConfigurationManager.AppSettings["strCtqyMultiCard"];

        private String strSecretkey = System.Configuration.ConfigurationManager.AppSettings["Secretkey"];
        private String strCertificateAddress = System.Configuration.ConfigurationManager.AppSettings["CertificateAddress"];
        private String strCertificatePassword = System.Configuration.ConfigurationManager.AppSettings["CertificatePassword"];
        private String strUseProxy = System.Configuration.ConfigurationManager.AppSettings["UseProxy"];

        private String strSqlConn_ActivateCard = ConfigurationManager.ConnectionStrings["SqlServer_ActivateCard"].ConnectionString;
        //private String strSqlConn_ShoppingCard = ConfigurationManager.ConnectionStrings["SqlServer_ShoppingCard"].ConnectionString;

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        /// <summary>
        /// 撤销（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public CancelResponse CancelRequestTest(string cardNo)
        {
            try
            {
                string issuerId = "C000";
                string merchantNo = "102932679765450";
                string operators = "ssh";
                //string cardNo = "2336659990000007387";
                string mac = getMd5Hash(issuerId + merchantNo + operators + cardNo + strPrivateKeyTraining);
                //string signType = "md5";
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strCancel);
                data.Append("?issuerId=" + issuerId);
                data.Append("&merchantNo=" + merchantNo);
                data.Append("&operator=" + operators);
                data.Append("&cardNo=" + cardNo);
                data.Append("&mac=" + mac);
                //data.Append("&signType=" + signType);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    CancelResponse resp = new CancelResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;

                        JArray ja = (JArray)jsonData["dataList"];
                        if (ja.Count > 0)
                        {
                            JObject jsonList = (JObject)ja[0];
                        }
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "CancelRequest", "撤销失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 撤销
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public CancelResponse CancelRequest(CancelInfo CClass)
        {
            try
            {
                string issuerId = CClass.IssuerId;
                string merchantNo = CClass.MerchantNo;
                string operators = CClass.Operators;
                string cardNo = CClass.CardNo;
                string mac = getMd5Hash(issuerId + merchantNo + operators + cardNo + strPrivateKeyTraining);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strCancel);
                data.Append("?issuerId=" + issuerId);
                data.Append("&merchantNo=" + merchantNo);
                data.Append("&operator=" + operators);
                data.Append("&cardNo=" + cardNo);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    CancelResponse resp = new CancelResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;

                        JArray ja = (JArray)jsonData["dataList"];
                        if (ja.Count > 0)
                        {
                            JObject jsonList = (JObject)ja[0];
                        }
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    Log2SQL(0, "CancelRequest", "撤销成功" + resp.CodeMsg.Msg + cardNo, address.AbsoluteUri);
                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "CancelRequest", "撤销失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 冻结解冻（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public FreezeResponse FreezeRequestTest(string cardNo)
        {
            try
            {
                string issuerId = "C000";
                string merchantNo = "102932679765450";
                string operators = "ssh";
                //string cardNo = "2336659991000008731";//2336659990000007387-2336659990000004475
                string freeze = "N";
                string mac = getMd5Hash(issuerId + merchantNo + operators + cardNo + freeze + strPrivateKeyTraining);
                //string signType = "md5";
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strFreeze);
                data.Append("?issuerId=" + issuerId);
                data.Append("&merchantNo=" + merchantNo);
                data.Append("&operator=" + operators);
                data.Append("&cardNo=" + cardNo);
                data.Append("&freeze=" + freeze);
                data.Append("&mac=" + mac);
                //data.Append("&signType=" + signType);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    FreezeResponse resp = new FreezeResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Msg = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "FreezeRequest", "冻结解冻失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 冻结解冻
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public FreezeResponse FreezeRequest(FreezeInfo CClass)
        {
            try
            {
                string issuerId = CClass.IssuerId;
                string merchantNo = CClass.MerchantNo;
                string operators = CClass.Operators;
                string cardNo = CClass.CardNo;
                string freeze = CClass.Freeze;
                string mac = getMd5Hash(issuerId + merchantNo + operators + cardNo + freeze + strPrivateKeyTraining);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strFreeze);
                data.Append("?issuerId=" + issuerId);
                data.Append("&merchantNo=" + merchantNo);
                data.Append("&operator=" + operators);
                data.Append("&cardNo=" + cardNo);
                data.Append("&freeze=" + freeze);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    FreezeResponse resp = new FreezeResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    Log2SQL(0, "FreezeRequest", "冻结解冻成功" + resp.CodeMsg.Msg + cardNo, address.AbsoluteUri);
                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "FreezeRequest", "冻结解冻失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 延期（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public PostPoneResponse PostPoneRequestTest(string cardNo)
        {
            try
            {
                string issuerId = "C000";
                string merchantNo = "102932679765450";
                string operators = "ssh";
                //string cardNo = "2336335500016443393";
                string date = "20180101";
                string mac = getMd5Hash(issuerId + merchantNo + operators + cardNo + date + strPrivateKeyTraining);
                //string signType = "md5";
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strPostPone);
                data.Append("?issuerId=" + issuerId);
                data.Append("&merchantNo=" + merchantNo);
                data.Append("&operator=" + operators);
                data.Append("&cardNo=" + cardNo);
                data.Append("&date=" + date);
                data.Append("&mac=" + mac);
                //data.Append("&signType=" + signType);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    PostPoneResponse resp = new PostPoneResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;

                        JArray ja = (JArray)jsonData["dataList"];
                        if (ja.Count > 0)
                        {
                            JObject jsonList = (JObject)ja[0];
                        }
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "PostPoneRequest", "延期失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 延期
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public PostPoneResponse PostPoneRequest(PostPoneInfo CClass)
        {
            try
            {
                string issuerId = CClass.IssuerId;
                string merchantNo = CClass.MerchantNo;
                string operators = CClass.Operators;
                string cardNo = CClass.CardNo;
                string date = CClass.Date;
                string mac = getMd5Hash(issuerId + merchantNo + operators + cardNo + date + strPrivateKeyTraining);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strPostPone);
                data.Append("?issuerId=" + issuerId);
                data.Append("&merchantNo=" + merchantNo);
                data.Append("&operator=" + operators);
                data.Append("&cardNo=" + cardNo);
                data.Append("&date=" + date);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    PostPoneResponse resp = new PostPoneResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;

                        JArray ja = (JArray)jsonData["dataList"];
                        if (ja.Count > 0)
                        {
                            JObject jsonList = (JObject)ja[0];
                        }
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    Log2SQL(0, "PostPoneRequest", "延期成功" + resp.CodeMsg.Msg + cardNo, address.AbsoluteUri);
                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "PostPoneRequest", "延期失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 查询（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public QueryOrderResponse QueryOrderRequestTest(string cardNo)
        {
            try
            {
                string issuerId = "C000";
                //cardNo = "2336659990000007478";//2336659991000008731-2336659990000004475-2336659990000007387
                string mac = getMd5Hash(issuerId + cardNo + strPrivateKeyTraining);
                //string signType = "md5";
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strQueryOrder);
                data.Append("?issuerId=" + issuerId);
                data.Append("&cardNo=" + cardNo);
                data.Append("&mac=" + mac);
                //data.Append("&signType=" + signType);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    QueryOrderResponse resp = new QueryOrderResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    string sMsg = (string)jsonData["msg"];
                    resp.CodeMsg.Code = sCode;
                    resp.CodeMsg.Msg = sMsg;
                    resp.Url = data.ToString();

                    if (sCode == "00")
                    {
                        JArray jaList = (JArray)jsonData.SelectToken("queryDataList");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string iCardNo = "";
                                string iCode = "";
                                string iMsg = "";
                                string iStatus = "";
                                string iBalance = "0";
                                string iExpiry = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { iCardNo = (string)joDetail["cardNo"]; }
                                catch { }
                                try
                                {
                                    iCode = (string)joDetail["code"];
                                }
                                catch { }
                                try
                                {
                                    iMsg = (string)joDetail["msg"];
                                }
                                catch { }
                                try
                                {
                                    iStatus = (string)joDetail["status"];
                                }
                                catch { }
                                try
                                {
                                    iBalance = (string)joDetail["balance"];
                                }
                                catch { }
                                try
                                {
                                    iExpiry = (string)joDetail["expiry"];
                                }
                                catch { }

                                DataListInfo dli = new DataListInfo();
                                dli.CardNo = iCardNo;
                                dli.Code = iCode;
                                dli.Msg = iMsg;
                                dli.Status = iStatus;
                                dli.Balance = iBalance == null ? "0" : iBalance;
                                dli.Expiry = iExpiry;
                                resp.DataList.Add(dli);
                            }
                        }
                    }
                    else
                    {
                        Log2SQL(0, "QueryOrderRequest", "查询订单失败" + sCode + sMsg, address.AbsoluteUri);
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "QueryOrderRequest", "查询订单失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public QueryOrderResponse QueryOrderRequest(QueryOrderInfo CClass)
        {
            try
            {
                string issuerId = CClass.IssuerId;
                string cardNo = CClass.CardNo;
                string mac = getMd5Hash(issuerId + cardNo + strPrivateKeyTraining);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strQueryOrder);
                data.Append("?issuerId=" + issuerId);
                data.Append("&cardNo=" + cardNo);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    QueryOrderResponse resp = new QueryOrderResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    string sMsg = (string)jsonData["msg"];
                    resp.CodeMsg.Code = sCode;
                    resp.CodeMsg.Msg = sMsg;
                    resp.Url = data.ToString();

                    if (sCode == "00")
                    {
                        JArray jaList = (JArray)jsonData.SelectToken("queryDataList");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string iCardNo = "";
                                string iCode = "";
                                string iMsg = "";
                                string iStatus = "";
                                string iBalance = "0";
                                string iExpiry = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { iCardNo = (string)joDetail["cardNo"]; }
                                catch { }
                                try
                                {
                                    iCode = (string)joDetail["code"];
                                }
                                catch { }
                                try
                                {
                                    iMsg = (string)joDetail["msg"];
                                }
                                catch { }
                                try
                                {
                                    iStatus = (string)joDetail["status"];
                                }
                                catch { }
                                try
                                {
                                    iBalance = (string)joDetail["balance"];
                                }
                                catch { }
                                try
                                {
                                    iExpiry = (string)joDetail["expiry"];
                                }
                                catch { }

                                DataListInfo dli = new DataListInfo();
                                dli.CardNo = iCardNo;
                                dli.Code = iCode;
                                dli.Msg = iMsg;
                                dli.Status = iStatus;
                                dli.Balance = iBalance == null ? "0" : iBalance;
                                dli.Expiry = iExpiry;
                                resp.DataList.Add(dli);
                            }
                        }

                        Log2SQL(0, "QueryOrderRequest", "查询订单成功" + sCode + sMsg + cardNo, address.AbsoluteUri);
                    }
                    else
                    {
                        Log2SQL(0, "QueryOrderRequest", "查询订单失败" + sCode + sMsg, address.AbsoluteUri);
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "QueryOrderRequest", "查询订单失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 补发短信（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public ResendMsgResponse ResendMsgRequestTest()
        {
            try
            {
                string issuerId = "C000";
                string mobile = "13901016791";
                string cardNo = "2336659990000007395";
                string operators = "ssh";
                string mac = getMd5Hash(issuerId + mobile + cardNo + strPrivateKeyTraining);
                //string signType = "md5";
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strResendMsgInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&mobile=" + mobile);
                data.Append("&cardNo=" + cardNo);
                data.Append("&operator=" + operators);
                data.Append("&mac=" + mac);
                //data.Append("&signType=" + signType);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    ResendMsgResponse resp = new ResendMsgResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "ResendMsgRequest", "补发短信失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 补发短信
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public ResendMsgResponse ResendMsgRequest(ResendMsgInfo CClass)
        {
            try
            {
                string issuerId = CClass.IssuerId;
                string mobile = CClass.Mobile;
                string cardNo = CClass.CardNo;
                string operators = CClass.Operators;
                string mac = getMd5Hash(issuerId + mobile + cardNo + strPrivateKeyTraining);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strResendMsgInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&mobile=" + mobile);
                data.Append("&cardNo=" + cardNo);
                data.Append("&operator=" + operators);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    ResendMsgResponse resp = new ResendMsgResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    Log2SQL(0, "ResendMsgRequest", "补发短信成功" + sCode + resp.CodeMsg.Msg + cardNo, address.AbsoluteUri);
                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "ResendMsgRequest", "补发短信失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 补发邮件（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public ResendMailResponse ResendMailRequestTest()
        {
            try
            {
                string issuerId = "C000";
                string orderNo = "20170509004";
                string user = "ssh";
                string mac = getMd5Hash(issuerId + orderNo + strPrivateKeyTraining);
                //string signType = "md5";
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strResendMailInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&orderNo=" + orderNo);
                data.Append("&user=" + user);
                data.Append("&mac=" + mac);
                //data.Append("&signType=" + signType);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    ResendMailResponse resp = new ResendMailResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "ResendMailRequest", "补发邮件失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 补发邮件
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public ResendMailResponse ResendMailRequest(ResendMailInfo CClass)
        {
            try
            {
                string issuerId = CClass.IssuerId;
                string orderNo = CClass.OrderNo;
                string user = CClass.User;
                string mac = getMd5Hash(issuerId + orderNo + strPrivateKeyTraining);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strResendMailInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&orderNo=" + orderNo);
                data.Append("&user=" + user);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    ResendMailResponse resp = new ResendMailResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    if (sCode == "00")
                    {
                        // 成功
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }
                    else
                    {
                        string sMsg = (string)jsonData["msg"];
                        resp.CodeMsg.Msg = sMsg;
                    }

                    Log2SQL(0, "ResendMailRequest", "补发邮件成功" + sCode + resp.CodeMsg.Msg + orderNo, address.AbsoluteUri);
                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "ResendMailRequest", "补发邮件失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 手机实时下单（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public MobileRealTimeResponse MobileRealTimeOrderRequestTest()
        {
            try
            {
                List<MobileDetailInfo> list = new List<MobileDetailInfo>();
                MobileDetailInfo detail = new MobileDetailInfo();
                detail.issuerId = "C000";
                detail.merchantNo = "102932679765450";//102210054110423
                detail.mobile = "15021034792";
                detail.shortUrlBeginDate = "20170704";
                detail.shortUrlEndDate = "20200704";
                detail.totalAmount = 100000;
                detail.availableCount = 999999;//可以使用次数
                detail.needVerifyCode = "N";//是否需要验证码
                detail.VerifyCodeLength = 4;//验证码长度
                detail.VerifyCodeAmount = 200;//验证码的触发金额
                detail.needFreezeCard = "";//是否需要冻结卡片
                detail.amount = 20000;
                detail.cardNumber = 5;
                list.Add(detail);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string objJson = serializer.Serialize(list);

                string issuerId = "C000";
                string orderNo = "2017062283";
                string orderType = "M";
                string date = DateTime.Now.ToString("yyyyMMdd");
                string mobileListJson = objJson;
                string mac = getMd5Hash(issuerId + orderNo + orderType + date + mobileListJson + strPrivateKeyProducingEx);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strPlaceMobileOrder);
                data.Append("?issuerId=" + issuerId);
                data.Append("&orderNo=" + orderNo);
                data.Append("&orderType=" + orderType);
                data.Append("&date=" + date);
                data.Append("&mobileListJson=" + mobileListJson);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    MobileRealTimeResponse resp = new MobileRealTimeResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;
                    string sMsg = (string)jsonData["msg"];
                    resp.CodeMsg.Msg = sMsg;

                    if (sCode == "00")
                    {
                        // 成功
                        JArray jaList = (JArray)jsonData.SelectToken("orderMobileDataList");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string iCode = "";
                                string iMsg = "";
                                string iIssuerId = "";
                                string iStartCardNo = "";
                                string iEndCardNo = "";
                                string iMobile = "";
                                string iAmount = "";
                                string iOrderNo = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { iCode = (string)joDetail["code"]; }
                                catch { }
                                try { iMsg = (string)joDetail["msg"]; }
                                catch { }
                                try { iIssuerId = (string)joDetail["issuerId"]; }
                                catch { }
                                try { iStartCardNo = (string)joDetail["startCardNo"]; }
                                catch { }
                                try { iEndCardNo = (string)joDetail["endCardNo"]; }
                                catch { }
                                try { iMobile = (string)joDetail["mobile"]; }
                                catch { }
                                try { iAmount = (((decimal)joDetail["amount"]) / 100).ToString(); }
                                catch { }
                                try { iOrderNo = (string)joDetail["orderNo"]; }
                                catch { }

                                DataListInfoByMobile li = new DataListInfoByMobile();
                                li.Code = iCode;
                                li.Msg = iMsg;
                                li.IssuerId = iIssuerId;
                                li.StartCardNo = iStartCardNo;
                                li.EndCardNo = iEndCardNo;
                                li.Amount = iAmount;
                                li.OrderNo = iOrderNo;
                                li.Mobile = iMobile;
                                li.CardNumber = detail.cardNumber;
                                li.FaceValue = detail.amount;
                                resp.DataList.Add(li);
                            }
                        }
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "MobileRealTimeOrderRequest", "手机实时下单失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 手机实时下单
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public MobileRealTimeResponse MobileRealTimeOrderRequest(MobileListJsonInfo CClass)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string objJson = serializer.Serialize(CClass.DataList);

                string issuerId = CClass.IssuerId;
                string orderNo = CClass.OrderNo;
                string orderType = CClass.OrderType;
                string date = CClass.Date;
                string mobileListJson = objJson;
                string mac = getMd5Hash(issuerId + orderNo + orderType + date + mobileListJson + strPrivateKeyProducingEx);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strPlaceMobileOrder);
                data.Append("?issuerId=" + issuerId);
                data.Append("&orderNo=" + orderNo);
                data.Append("&orderType=" + orderType);
                data.Append("&date=" + date);
                data.Append("&mobileListJson=" + mobileListJson);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    MobileRealTimeResponse resp = new MobileRealTimeResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;
                    string sMsg = (string)jsonData["msg"];
                    resp.CodeMsg.Msg = sMsg;

                    if (sCode == "00")
                    {
                        // 成功
                        JArray jaList = (JArray)jsonData.SelectToken("orderMobileDataList");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string iCode = "";
                                string iMsg = "";
                                string iIssuerId = "";
                                string iStartCardNo = "";
                                string iEndCardNo = "";
                                string iMobile = "";
                                string iAmount = "";
                                string iOrderNo = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { iCode = (string)joDetail["code"]; }
                                catch { }
                                try { iMsg = (string)joDetail["msg"]; }
                                catch { }
                                try { iIssuerId = (string)joDetail["issuerId"]; }
                                catch { }
                                try { iStartCardNo = (string)joDetail["startCardNo"]; }
                                catch { }
                                try { iEndCardNo = (string)joDetail["endCardNo"]; }
                                catch { }
                                try { iMobile = (string)joDetail["mobile"]; }
                                catch { }
                                try { iAmount = (((decimal)joDetail["amount"]) / 100).ToString(); }
                                catch { }
                                try { iOrderNo = (string)joDetail["orderNo"]; }
                                catch { }

                                DataListInfoByMobile li = new DataListInfoByMobile();
                                li.Code = iCode;
                                li.Msg = iMsg;
                                li.IssuerId = iIssuerId;
                                li.StartCardNo = iStartCardNo;
                                li.EndCardNo = iEndCardNo;
                                li.Amount = iAmount;
                                li.OrderNo = iOrderNo;
                                li.Mobile = iMobile;
                                li.CardNumber = CClass.DataList[i].cardNumber;
                                li.FaceValue = int.Parse(iAmount);
                                resp.DataList.Add(li);
                            }
                        }
                    }

                    Log2SQL(0, "MobileRealTimeOrderRequest", "手机实时下单成功" + sCode + resp.CodeMsg.Msg, address.AbsoluteUri);
                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "MobileRealTimeOrderRequest", "手机实时下单失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 邮箱实时下单（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public MailRealTimeResponse MailRealTimeOrderRequestTest()
        {
            try
            {
                List<MailDetailInfo> list = new List<MailDetailInfo>();
                MailDetailInfo detail = new MailDetailInfo();
                detail.issuerId = "C000";
                detail.merchantNo = "102210054110423";
                detail.mail = "cwhj_qipeng@163.com";
                detail.mobile = "15021034792";
                detail.shortUrlBeginDate = "20170102";
                detail.shortUrlEndDate = "20180102";
                detail.totalAmount = 40000;
                detail.availableCount = 999999;//可以使用次数
                detail.needVerifyCode = "N";//是否需要验证码
                detail.VerifyCodeLength = 4;//验证码长度
                detail.VerifyCodeAmount = 200;//验证码的触发金额
                detail.needFreezeCard = "";//是否需要冻结卡片
                detail.amount = 20000;
                detail.cardNumber = 2;
                list.Add(detail);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string objJson = serializer.Serialize(list);

                string issuerId = "C000";
                string orderNo = "20171202095";
                string orderType = "F";
                string date = DateTime.Now.ToString("yyyyMMdd");
                string detailListJson = objJson;
                string mac = getMd5Hash(issuerId + orderNo + orderType + date + detailListJson + strPrivateKeyProducingEx);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strPlaceExcelOrder);
                data.Append("?issuerId=" + issuerId);
                data.Append("&orderNo=" + orderNo);
                data.Append("&orderType=" + orderType);
                data.Append("&date=" + date);
                data.Append("&detailListJson=" + detailListJson);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    MailRealTimeResponse resp = new MailRealTimeResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;

                    string sMsg = (string)jsonData["msg"];
                    resp.CodeMsg.Msg = sMsg;

                    if (sCode == "00")
                    {
                        // 成功
                        JArray jaList = (JArray)jsonData.SelectToken("orderDataList");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string iCode = "";
                                string iMsg = "";
                                string iIssuerId = "";
                                string iStartCardNo = "";
                                string iEndCardNo = "";
                                string iAmount = "";
                                string iOrderNo = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { iCode = (string)joDetail["code"]; }
                                catch { }
                                try { iMsg = (string)joDetail["msg"]; }
                                catch { }
                                try { iIssuerId = (string)joDetail["issuerId"]; }
                                catch { }
                                try { iStartCardNo = (string)joDetail["startCardNo"]; }
                                catch { }
                                try { iEndCardNo = (string)joDetail["endCardNo"]; }
                                catch { }
                                try { iAmount = (((decimal)joDetail["amount"]) / 100).ToString(); }
                                catch { }
                                try { iOrderNo = (string)joDetail["orderNo"]; }
                                catch { }

                                DataListInfoByMail li = new DataListInfoByMail();
                                li.Code = iCode;
                                li.Msg = iMsg;
                                li.IssuerId = iIssuerId;
                                li.StartCardNo = iStartCardNo;
                                li.EndCardNo = iEndCardNo;
                                li.Amount = iAmount;
                                li.OrderNo = iOrderNo;
                                li.CardNumber = detail.cardNumber;
                                li.FaceValue = detail.amount;
                                resp.DataList.Add(li);
                            }
                        }
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "MailRealTimeOrderRequest", "邮箱实时下单失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 邮箱实时下单
        /// </summary>
        /// <param name="CClass"></param>
        /// <returns></returns>
        [WebMethod]
        public MailRealTimeResponse MailRealTimeOrderRequest(MailListJsonInfo CClass)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string objJson = serializer.Serialize(CClass.DataList);
                string issuerId = CClass.IssuerId;
                string orderNo = CClass.OrderNo;
                string orderType = CClass.OrderType;
                string date = CClass.Date;
                string detailListJson = objJson;
                string mac = getMd5Hash(issuerId + orderNo + orderType + date + detailListJson + strPrivateKeyProducingEx);
                string type = "json";

                StringBuilder data = new StringBuilder();
                data.Append(strPlaceExcelOrder);
                data.Append("?issuerId=" + issuerId);
                data.Append("&orderNo=" + orderNo);
                data.Append("&orderType=" + orderType);
                data.Append("&date=" + date);
                data.Append("&detailListJson=" + detailListJson);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    MailRealTimeResponse resp = new MailRealTimeResponse();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    resp.CodeMsg.Code = sCode;
                    string sMsg = (string)jsonData["msg"];
                    resp.CodeMsg.Msg = sMsg;

                    if (sCode == "00")
                    {
                        // 成功
                        JArray jaList = (JArray)jsonData.SelectToken("orderDataList");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string iCode = "";
                                string iMsg = "";
                                string iIssuerId = "";
                                string iStartCardNo = "";
                                string iEndCardNo = "";
                                string iAmount = "";
                                string iOrderNo = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { iCode = (string)joDetail["code"]; }
                                catch { }
                                try { iMsg = (string)joDetail["msg"]; }
                                catch { }
                                try { iIssuerId = (string)joDetail["issuerId"]; }
                                catch { }
                                try { iStartCardNo = (string)joDetail["startCardNo"]; }
                                catch { }
                                try { iEndCardNo = (string)joDetail["endCardNo"]; }
                                catch { }
                                try { iAmount = (((decimal)joDetail["amount"]) / 100).ToString(); }
                                catch { }
                                try { iOrderNo = (string)joDetail["orderNo"]; }
                                catch { }

                                DataListInfoByMail li = new DataListInfoByMail();
                                li.Code = iCode;
                                li.Msg = iMsg;
                                li.IssuerId = iIssuerId;
                                li.StartCardNo = iStartCardNo;
                                li.EndCardNo = iEndCardNo;
                                li.Amount = iAmount;
                                li.OrderNo = iOrderNo;
                                li.CardNumber = CClass.DataList[i].cardNumber;
                                li.FaceValue = int.Parse(iAmount);
                                resp.DataList.Add(li);
                            }
                        }
                    }

                    Log2SQL(0, "MailRealTimeOrderRequest", "邮箱实时下单成功" + sCode + resp.CodeMsg.Msg, address.AbsoluteUri);
                    return resp;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "MailRealTimeOrderRequest", "邮箱实时下单失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 交易明细查询（测试）
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public CtqyMultiDataClass CtqyMultiCardDataTest(string sCardNo, string sMerchantNo)
        {
            try
            {
                List<CtqyMultiCardInfo> list = new List<CtqyMultiCardInfo>();
                CtqyMultiCardInfo cmci = new CtqyMultiCardInfo();
                cmci.cardNo = sCardNo; //2336501170001403031//2336840209900006071
                cmci.passwordType = "";
                cmci.password = "";
                list.Add(cmci);

                CtqyMultiCardBean cmc = new CtqyMultiCardBean();
                cmc.merchantNo = sMerchantNo;//102021000030080//102210054110423
                cmc.userId = sMerchantNo;
                cmc.cardInfos = list;
                cmc.isVerifyPassword = "N";
                cmc.queryType = "H";
                cmc.dateFrom = "";
                cmc.dateTo = "";
                cmc.isPager = "N";
                cmc.pageNo = "1";
                cmc.pageSize = "1000000";
                cmc.sortRule = "";

                string secretkey = strSecretkey;
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string objJson = serializer.Serialize(cmc);
                string macData = getMd5Hash(secretkey + serializer.Serialize(cmc));
                StringBuilder data = new StringBuilder();
                data.Append(strCtqyMultiCard);
                data.Append(macData);
                data.Append("?ctqyMultiCard=" + objJson);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证

                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }

                //, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable
                X509Certificate clientCert = new X509Certificate2(strCertificateAddress, "c4pjlfzb&123");//c4pjlfzb&123
                request.ClientCertificates.Add(clientCert);
                request.Method = "POST";

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    CtqyMultiDataClass cmd = new CtqyMultiDataClass();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    cmd.code = sCode;
                    string sMsg = (string)jsonData["msg"];
                    cmd.msg = sMsg;
                    string sTotal = (string)jsonData["total"];
                    cmd.total = sTotal;

                    if (sCode == "00")
                    {
                        // 成功
                        JArray jaList = (JArray)jsonData.SelectToken("cardTxns");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string txnId = "";
                                string cardNo = "";
                                string inputChannel = "";
                                string inputChannelName = "";
                                string txnCode = "";
                                string txnName = "";
                                string earnPoints = "";
                                string earnAmount = "";
                                string redeemPoints = "";
                                string redeemAmount = "";
                                string transferPoints = "";
                                string transferAmount = "";
                                string adjustPoints = "";
                                string adjustAmount = "";
                                string upAmount = "";
                                string upPoints = "";
                                string downAmount = "";
                                string downPoints = "";
                                string txnPoints = "";
                                string txnAmount = "";
                                string txnDate = "";
                                string txnTime = "";
                                string corporateNo = "";
                                string corporateName = "";
                                string merchantNo = "";
                                string merchantName = "";
                                string termNo = "";
                                string balanceAmount = "";
                                string balancePoints = "";
                                string remarks = "";
                                string rrn = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { txnId = (string)joDetail["txnId"]; }
                                catch { }
                                try { cardNo = (string)joDetail["cardNo"]; }
                                catch { }
                                try { inputChannel = (string)joDetail["inputChannel"]; }
                                catch { }
                                try { inputChannelName = (string)joDetail["inputChannelName"]; }
                                catch { }
                                try { txnCode = (string)joDetail["txnCode"]; }
                                catch { }
                                try { txnName = (string)joDetail["txnName"]; }
                                catch { }
                                try { earnPoints = (string)joDetail["earnPoints"]; }
                                catch { }
                                try { earnAmount = (string)joDetail["earnAmount"]; }
                                catch { }
                                try { redeemPoints = (string)joDetail["redeemPoints"]; }
                                catch { }
                                try { redeemAmount = (string)joDetail["redeemAmount"]; }
                                catch { }
                                try { transferPoints = (string)joDetail["transferPoints"]; }
                                catch { }
                                try { transferAmount = (string)joDetail["transferAmount"]; }
                                catch { }
                                try { adjustPoints = (string)joDetail["adjustPoints"]; }
                                catch { }
                                try { adjustAmount = (string)joDetail["adjustAmount"]; }
                                catch { }
                                try { upAmount = (string)joDetail["upAmount"]; }
                                catch { }
                                try { upPoints = (string)joDetail["upPoints"]; }
                                catch { }
                                try { downAmount = (string)joDetail["downAmount"]; }
                                catch { }
                                try { downPoints = (string)joDetail["downPoints"]; }
                                catch { }
                                try { txnPoints = (string)joDetail["txnPoints"]; }
                                catch { }
                                try { txnAmount = (string)joDetail["txnAmount"]; }
                                catch { }
                                try { txnDate = (string)joDetail["txnDate"]; }
                                catch { }
                                try { txnTime = (string)joDetail["txnTime"]; }
                                catch { }
                                try { corporateNo = (string)joDetail["corporateNo"]; }
                                catch { }
                                try { corporateName = (string)joDetail["corporateName"]; }
                                catch { }
                                try { merchantNo = (string)joDetail["merchantNo"]; }
                                catch { }
                                try { merchantName = (string)joDetail["merchantName"]; }
                                catch { }
                                try { termNo = (string)joDetail["termNo"]; }
                                catch { }
                                try { balanceAmount = (string)joDetail["balanceAmount"]; }
                                catch { }
                                try { balancePoints = (string)joDetail["balancePoints"]; }
                                catch { }
                                try { remarks = (string)joDetail["remarks"]; }
                                catch { }
                                try { rrn = (string)joDetail["rrn"]; }
                                catch { }

                                CardsTxnAll cta = new CardsTxnAll();
                                cta.txnId = txnId;
                                cta.cardNo = cardNo;
                                cta.inputChannel = inputChannel;
                                cta.inputChannelName = inputChannelName;
                                cta.txnCode = txnCode;
                                cta.txnName = txnName;
                                cta.earnPoints = earnPoints;
                                cta.earnAmount = earnAmount;
                                cta.redeemPoints = redeemPoints;
                                cta.redeemAmount = redeemAmount;
                                cta.transferPoints = transferPoints;
                                cta.transferAmount = transferAmount;
                                cta.adjustPoints = adjustPoints;
                                cta.adjustAmount = adjustAmount;
                                cta.upAmount = upAmount;
                                cta.upPoints = upPoints;
                                cta.downAmount = downAmount;
                                cta.downPoints = downPoints;
                                cta.txnPoints = txnPoints;
                                cta.txnAmount = txnAmount;
                                cta.txnDate = txnDate;
                                cta.txnTime = txnTime;
                                cta.corporateNo = corporateNo;
                                cta.corporateName = corporateName;
                                cta.merchantNo = merchantNo;
                                cta.merchantName = merchantName;
                                cta.termNo = termNo;
                                cta.balanceAmount = balanceAmount;
                                cta.balancePoints = balancePoints;
                                cta.remarks = remarks;
                                cta.rrn = rrn;
                                cmd.cardTxns.Add(cta);
                            }
                        }

                        Log2SQL(0, "CtqyMultiCardData", "多卡交易明细查询成功", address.AbsoluteUri);
                        return cmd;
                    }
                    else
                    {
                        Log2SQL(0, "CtqyMultiCardData", "多卡交易明细查询失败" + sMsg, address.AbsoluteUri);
                        return cmd;
                    }
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "CtqyMultiCardData", "多卡交易明细查询失败" + e.Message, address.AbsoluteUri + e.Source);
                throw e;
            }
        }

        /// <summary>
        /// 交易明细查询
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public CtqyMultiDataClass CtqyMultiCardData(CtqyMultiCardBean cmc)
        {
            try
            {
                string secretkey = strSecretkey;
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string objJson = serializer.Serialize(cmc);
                string macData = getMd5Hash(secretkey + serializer.Serialize(cmc));
                StringBuilder data = new StringBuilder();
                data.Append(strCtqyMultiCard);
                data.Append(macData);
                data.Append("?ctqyMultiCard=" + objJson);

                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);//验证服务器证书回调自动验证

                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }

                X509Certificate clientCert = new X509Certificate2(strCertificateAddress, "c4pjlfzb&123");
                request.ClientCertificates.Add(clientCert);
                request.Method = "POST";

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();

                    CtqyMultiDataClass cmd = new CtqyMultiDataClass();
                    JObject jsonData = (JObject)JsonConvert.DeserializeObject(result);
                    string sCode = (string)jsonData["code"];
                    cmd.code = sCode;
                    string sMsg = (string)jsonData["msg"];
                    cmd.msg = sMsg;
                    string sTotal = (string)jsonData["total"];
                    cmd.total = sTotal;

                    if (sCode == "00")
                    {
                        // 成功
                        JArray jaList = (JArray)jsonData.SelectToken("cardTxns");
                        if (jaList.Count > 0)
                        {
                            for (int i = 0; i < jaList.Count; i++)
                            {
                                string txnId = "";
                                string cardNo = "";
                                string inputChannel = "";
                                string inputChannelName = "";
                                string txnCode = "";
                                string txnName = "";
                                string earnPoints = "";
                                string earnAmount = "";
                                string redeemPoints = "";
                                string redeemAmount = "";
                                string transferPoints = "";
                                string transferAmount = "";
                                string adjustPoints = "";
                                string adjustAmount = "";
                                string upAmount = "";
                                string upPoints = "";
                                string downAmount = "";
                                string downPoints = "";
                                string txnPoints = "";
                                string txnAmount = "";
                                string txnDate = "";
                                string txnTime = "";
                                string corporateNo = "";
                                string corporateName = "";
                                string merchantNo = "";
                                string merchantName = "";
                                string termNo = "";
                                string balanceAmount = "";
                                string balancePoints = "";
                                string remarks = "";
                                string rrn = "";
                                JObject joDetail = (JObject)jaList[i];

                                try { txnId = (string)joDetail["txnId"]; }
                                catch { }
                                try { cardNo = (string)joDetail["cardNo"]; }
                                catch { }
                                try { inputChannel = (string)joDetail["inputChannel"]; }
                                catch { }
                                try { inputChannelName = (string)joDetail["inputChannelName"]; }
                                catch { }
                                try { txnCode = (string)joDetail["txnCode"]; }
                                catch { }
                                try { txnName = (string)joDetail["txnName"]; }
                                catch { }
                                try { earnPoints = (string)joDetail["earnPoints"]; }
                                catch { }
                                try { earnAmount = (string)joDetail["earnAmount"]; }
                                catch { }
                                try { redeemPoints = (string)joDetail["redeemPoints"]; }
                                catch { }
                                try { redeemAmount = (string)joDetail["redeemAmount"]; }
                                catch { }
                                try { transferPoints = (string)joDetail["transferPoints"]; }
                                catch { }
                                try { transferAmount = (string)joDetail["transferAmount"]; }
                                catch { }
                                try { adjustPoints = (string)joDetail["adjustPoints"]; }
                                catch { }
                                try { adjustAmount = (string)joDetail["adjustAmount"]; }
                                catch { }
                                try { upAmount = (string)joDetail["upAmount"]; }
                                catch { }
                                try { upPoints = (string)joDetail["upPoints"]; }
                                catch { }
                                try { downAmount = (string)joDetail["downAmount"]; }
                                catch { }
                                try { downPoints = (string)joDetail["downPoints"]; }
                                catch { }
                                try { txnPoints = (string)joDetail["txnPoints"]; }
                                catch { }
                                try { txnAmount = (string)joDetail["txnAmount"]; }
                                catch { }
                                try { txnDate = (string)joDetail["txnDate"]; }
                                catch { }
                                try { txnTime = (string)joDetail["txnTime"]; }
                                catch { }
                                try { corporateNo = (string)joDetail["corporateNo"]; }
                                catch { }
                                try { corporateName = (string)joDetail["corporateName"]; }
                                catch { }
                                try { merchantNo = (string)joDetail["merchantNo"]; }
                                catch { }
                                try { merchantName = (string)joDetail["merchantName"]; }
                                catch { }
                                try { termNo = (string)joDetail["termNo"]; }
                                catch { }
                                try { balanceAmount = (string)joDetail["balanceAmount"]; }
                                catch { }
                                try { balancePoints = (string)joDetail["balancePoints"]; }
                                catch { }
                                try { remarks = (string)joDetail["remarks"]; }
                                catch { }
                                try { rrn = (string)joDetail["rrn"]; }
                                catch { }

                                CardsTxnAll cta = new CardsTxnAll();
                                cta.txnId = txnId;
                                cta.cardNo = cardNo;
                                cta.inputChannel = inputChannel;
                                cta.inputChannelName = inputChannelName;
                                cta.txnCode = txnCode;
                                cta.txnName = txnName;
                                cta.earnPoints = earnPoints;
                                cta.earnAmount = earnAmount;
                                cta.redeemPoints = redeemPoints;
                                cta.redeemAmount = redeemAmount;
                                cta.transferPoints = transferPoints;
                                cta.transferAmount = transferAmount;
                                cta.adjustPoints = adjustPoints;
                                cta.adjustAmount = adjustAmount;
                                cta.upAmount = upAmount;
                                cta.upPoints = upPoints;
                                cta.downAmount = downAmount;
                                cta.downPoints = downPoints;
                                cta.txnPoints = txnPoints;
                                cta.txnAmount = txnAmount;
                                cta.txnDate = txnDate;
                                cta.txnTime = txnTime;
                                cta.corporateNo = corporateNo;
                                cta.corporateName = corporateName;
                                cta.merchantNo = merchantNo;
                                cta.merchantName = merchantName;
                                cta.termNo = termNo;
                                cta.balanceAmount = balanceAmount;
                                cta.balancePoints = balancePoints;
                                cta.remarks = remarks;
                                cta.rrn = rrn;
                                cmd.cardTxns.Add(cta);
                            }
                        }

                        Log2SQL(0, "CtqyMultiCardData", "多卡交易明细查询成功", address.AbsoluteUri);
                        return cmd;
                    }
                    else
                    {
                        Log2SQL(0, "CtqyMultiCardData", "多卡交易明细查询失败" + sMsg, address.AbsoluteUri);
                        return cmd;
                    }
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "CtqyMultiCardData", "多卡交易明细查询失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        /// <summary>
        /// 获取代理信息
        /// </summary>
        /// <returns></returns>
        private WebProxy getProxy()
        {
            string strProxyHost = System.Configuration.ConfigurationManager.AppSettings["ProxyHost"];
            int intProxyPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["ProxyPort"]);
            string strProxyUserName = System.Configuration.ConfigurationManager.AppSettings["ProxyUserName"];
            string strProxyPassword = System.Configuration.ConfigurationManager.AppSettings["ProxyPassword"];

            string strProxyPasswordDec = Decrypt(strProxyPassword, "5rdx*IK<");
            WebProxy Proxy = new WebProxy(strProxyHost, intProxyPort);
            Proxy.Credentials = new NetworkCredential(strProxyUserName, strProxyPasswordDec);
            return Proxy;
        }

        /// <summary>
        /// MD5解密
        /// </summary>
        /// <param name="strText"></param>
        /// <param name="sDecrKey"></param>
        /// <returns></returns>
        private string Decrypt(String strText, String sDecrKey)
        {
            Byte[] byKey = { };
            Byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            Byte[] inputByteArray = new byte[strText.Length];
            try
            {
                byKey = System.Text.Encoding.UTF8.GetBytes(sDecrKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Convert.FromBase64String(strText);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(byKey, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                System.Text.Encoding encoding = System.Text.Encoding.UTF8;
                return encoding.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// 得到MD5值
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string getMd5Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                //X2是左边补零的十六进制
                sBuilder.Append(data[i].ToString("X2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
            //{8b1a9953c4611296a827abf8c47804d7}
            //{8B1A9953C4611296A827ABF8C4784D7}
        }

        /// <summary>
        /// 写数据库日志
        /// </summary>
        /// <param name="intLevel"></param>
        /// <param name="strMothed"></param>
        /// <param name="strMessage"></param>
        /// <param name="strUrl"></param>
        private void Log2SQL(int intLevel, string strMothed, string strMessage, string strUrl)
        {
            string strLevel = "";
            if (intLevel == 0)
            {
                strLevel = "Info";
            }
            else if (intLevel == 1)
            {
                strLevel = "Error";
            }
            else
            {
                strLevel = "Other";
            }

            using (SqlConnection sqlConn = new SqlConnection(strSqlConn_ActivateCard))
            {
                using (SqlCommand sqlComm = new SqlCommand())
                {
                    sqlConn.Open();
                    sqlComm.Connection = sqlConn;
                    sqlComm.CommandText = String.Format("insert into ActivationLogForInternetSales(Date,Level,Mothed,Message,Url) values('{0}','{1}','{2}','{3}','{4}')", DateTime.Now.ToString(), strLevel, strMothed, strMessage, strUrl);
                    sqlComm.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 检查证书
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            // 总是接受    
            return true;
        }


    }
    public class MobileDetailInfo
    {
        private string _issuerId;

        public string issuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _merchantNo;

        public string merchantNo
        {
            get { return _merchantNo; }
            set { _merchantNo = value; }
        }
        private string _mobile;

        public string mobile
        {
            get { return _mobile; }
            set { _mobile = value; }
        }
        private string _shortUrlBeginDate;

        public string shortUrlBeginDate
        {
            get { return _shortUrlBeginDate; }
            set { _shortUrlBeginDate = value; }
        }
        private string _shortUrlEndDate;

        public string shortUrlEndDate
        {
            get { return _shortUrlEndDate; }
            set { _shortUrlEndDate = value; }
        }
        private int _totalAmount;

        public int totalAmount
        {
            get { return _totalAmount; }
            set { _totalAmount = value; }
        }
        private int _availableCount;

        public int availableCount
        {
            get { return _availableCount; }
            set { _availableCount = value; }
        }
        private string _needVerifyCode;

        public string needVerifyCode
        {
            get { return _needVerifyCode; }
            set { _needVerifyCode = value; }
        }
        private int _verifyCodeLength;

        public int VerifyCodeLength
        {
            get { return _verifyCodeLength; }
            set { _verifyCodeLength = value; }
        }
        private int _verifyCodeAmount;

        public int VerifyCodeAmount
        {
            get { return _verifyCodeAmount; }
            set { _verifyCodeAmount = value; }
        }
        private string _needFreezeCard;

        public string needFreezeCard
        {
            get { return _needFreezeCard; }
            set { _needFreezeCard = value; }
        }
        private int _amount;

        public int amount
        {
            get { return _amount; }
            set { _amount = value; }
        }
        private int _cardNumber;

        public int cardNumber
        {
            get { return _cardNumber; }
            set { _cardNumber = value; }
        }
        public MobileDetailInfo() { }
    }
    public class MailDetailInfo
    {
        private string _issuerId;

        public string issuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _merchantNo;

        public string merchantNo
        {
            get { return _merchantNo; }
            set { _merchantNo = value; }
        }
        private string _mail;

        public string mail
        {
            get { return _mail; }
            set { _mail = value; }
        }
        private string _mobile;

        public string mobile
        {
            get { return _mobile; }
            set { _mobile = value; }
        }
        private string _shortUrlBeginDate;

        public string shortUrlBeginDate
        {
            get { return _shortUrlBeginDate; }
            set { _shortUrlBeginDate = value; }
        }
        private string _shortUrlEndDate;

        public string shortUrlEndDate
        {
            get { return _shortUrlEndDate; }
            set { _shortUrlEndDate = value; }
        }
        private int _totalAmount;

        public int totalAmount
        {
            get { return _totalAmount; }
            set { _totalAmount = value; }
        }
        private int _availableCount;

        public int availableCount
        {
            get { return _availableCount; }
            set { _availableCount = value; }
        }
        private string _needVerifyCode;

        public string needVerifyCode
        {
            get { return _needVerifyCode; }
            set { _needVerifyCode = value; }
        }
        private int _verifyCodeLength;

        public int VerifyCodeLength
        {
            get { return _verifyCodeLength; }
            set { _verifyCodeLength = value; }
        }
        private int _verifyCodeAmount;

        public int VerifyCodeAmount
        {
            get { return _verifyCodeAmount; }
            set { _verifyCodeAmount = value; }
        }
        private string _needFreezeCard;

        public string needFreezeCard
        {
            get { return _needFreezeCard; }
            set { _needFreezeCard = value; }
        }
        private int _amount;

        public int amount
        {
            get { return _amount; }
            set { _amount = value; }
        }
        private int _cardNumber;

        public int cardNumber
        {
            get { return _cardNumber; }
            set { _cardNumber = value; }
        }
        public MailDetailInfo() { }
    }
    public class CancelInfo
    {
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _merchantNo;

        public string MerchantNo
        {
            get { return _merchantNo; }
            set { _merchantNo = value; }
        }
        private string _operators;

        public string Operators
        {
            get { return _operators; }
            set { _operators = value; }
        }
        private string _cardNo;

        public string CardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; }
        }
        public CancelInfo()
        {

        }
    }
    public class FreezeInfo
    {
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _merchantNo;

        public string MerchantNo
        {
            get { return _merchantNo; }
            set { _merchantNo = value; }
        }
        private string _operators;

        public string Operators
        {
            get { return _operators; }
            set { _operators = value; }
        }
        private string _cardNo;

        public string CardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; }
        }
        private string _freeze;

        public string Freeze
        {
            get { return _freeze; }
            set { _freeze = value; }
        }
        public FreezeInfo()
        {

        }
    }
    public class PostPoneInfo
    {
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _merchantNo;

        public string MerchantNo
        {
            get { return _merchantNo; }
            set { _merchantNo = value; }
        }
        private string _operators;

        public string Operators
        {
            get { return _operators; }
            set { _operators = value; }
        }
        private string _cardNo;

        public string CardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; }
        }
        private string _date;

        public string Date
        {
            get { return _date; }
            set { _date = value; }
        }
        public PostPoneInfo() { }
    }
    public class QueryOrderInfo
    {
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _cardNo;

        public string CardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; }
        }
        public QueryOrderInfo() { }
    }
    public class ResendMsgInfo
    {
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _mobile;

        public string Mobile
        {
            get { return _mobile; }
            set { _mobile = value; }
        }
        private string _cardNo;

        public string CardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; }
        }
        private string _operators;

        public string Operators
        {
            get { return _operators; }
            set { _operators = value; }
        }
        public ResendMsgInfo() { }
    }
    public class ResendMailInfo
    {
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _orderNo;

        public string OrderNo
        {
            get { return _orderNo; }
            set { _orderNo = value; }
        }
        private string _user;

        public string User
        {
            get { return _user; }
            set { _user = value; }
        }
        public ResendMailInfo() { }
    }
    public class MobileListJsonInfo
    {
        private string _orderNo;

        public string OrderNo
        {
            get { return _orderNo; }
            set { _orderNo = value; }
        }
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _orderType;

        public string OrderType
        {
            get { return _orderType; }
            set { _orderType = value; }
        }
        private string _date;

        public string Date
        {
            get { return _date; }
            set { _date = value; }
        }
        private List<MobileDetailInfo> _dataList;

        public List<MobileDetailInfo> DataList
        {
            get { return _dataList; }
            set { _dataList = value; }
        }
        public MobileListJsonInfo()
        {
            _dataList = new List<MobileDetailInfo>();
        }
    }
    public class MailListJsonInfo
    {
        private string _orderNo;

        public string OrderNo
        {
            get { return _orderNo; }
            set { _orderNo = value; }
        }
        private string _issuerId;

        public string IssuerId
        {
            get { return _issuerId; }
            set { _issuerId = value; }
        }
        private string _orderType;

        public string OrderType
        {
            get { return _orderType; }
            set { _orderType = value; }
        }
        private string _date;

        public string Date
        {
            get { return _date; }
            set { _date = value; }
        }
        private List<MailDetailInfo> _dataList;

        public List<MailDetailInfo> DataList
        {
            get { return _dataList; }
            set { _dataList = value; }
        }
        public MailListJsonInfo()
        {
            _dataList = new List<MailDetailInfo>();
        }
    }

    public class CodeMsg
    {
        public CodeMsg() { }
        private string code; public string Code { get { return code; } set { code = value; } }
        private string msg; public string Msg { get { return msg; } set { msg = value; } }
    }
    public class DataListInfo
    {
        public DataListInfo() { }
        private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
        private string code; public string Code { get { return code; } set { code = value; } }
        private string msg; public string Msg { get { return msg; } set { msg = value; } }
        private string status; public string Status { get { return status; } set { status = value; } }
        private string balance; public string Balance { get { return balance; } set { balance = value; } }
        private string expiry; public string Expiry { get { return expiry; } set { expiry = value; } }
    }
    public class DataListInfoByMobile
    {
        public DataListInfoByMobile() { }
        private string code; public string Code { get { return code; } set { code = value; } }
        private string msg; public string Msg { get { return msg; } set { msg = value; } }
        private string issuerId; public string IssuerId { get { return issuerId; } set { issuerId = value; } }
        private string orderNo; public string OrderNo { get { return orderNo; } set { orderNo = value; } }
        private string startCardNo; public string StartCardNo { get { return startCardNo; } set { startCardNo = value; } }
        private string endCardNo; public string EndCardNo { get { return endCardNo; } set { endCardNo = value; } }
        private string amount; public string Amount { get { return amount; } set { amount = value; } }
        private string mobile; public string Mobile { get { return mobile; } set { mobile = value; } }
        private int cardNumber; public int CardNumber { get { return cardNumber; } set { cardNumber = value; } }
        private int faceValue; public int FaceValue { get { return faceValue; } set { faceValue = value; } }
    }
    public class DataListInfoByMail
    {
        public DataListInfoByMail() { }
        private string code; public string Code { get { return code; } set { code = value; } }
        private string msg; public string Msg { get { return msg; } set { msg = value; } }
        private string issuerId; public string IssuerId { get { return issuerId; } set { issuerId = value; } }
        private string orderNo; public string OrderNo { get { return orderNo; } set { orderNo = value; } }
        private string startCardNo; public string StartCardNo { get { return startCardNo; } set { startCardNo = value; } }
        private string endCardNo; public string EndCardNo { get { return endCardNo; } set { endCardNo = value; } }
        private string amount; public string Amount { get { return amount; } set { amount = value; } }
        private int cardNumber; public int CardNumber { get { return cardNumber; } set { cardNumber = value; } }
        private int faceValue; public int FaceValue { get { return faceValue; } set { faceValue = value; } }
    }
    public class CancelResponse
    {
        public CancelResponse()
        {
            codeMsg = new CodeMsg();
        }

        private CodeMsg codeMsg;

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
    }
    public class FreezeResponse
    {
        public FreezeResponse()
        {
            codeMsg = new CodeMsg();
        }

        private CodeMsg codeMsg;

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
    }
    public class PostPoneResponse
    {
        public PostPoneResponse()
        {
            codeMsg = new CodeMsg();
        }

        private CodeMsg codeMsg;

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
    }
    public class QueryOrderResponse
    {
        public QueryOrderResponse()
        {
            url = Url;
            codeMsg = new CodeMsg();
            dataList = new List<DataListInfo>();
        }

        private string url;
        private CodeMsg codeMsg;
        private List<DataListInfo> dataList;

        public string Url { get { return url; } set { url = value; } }
        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
        public List<DataListInfo> DataList { get { return dataList; } set { dataList = value; } }
    }
    public class ResendMsgResponse
    {
        public ResendMsgResponse()
        {
            codeMsg = new CodeMsg();
        }

        private CodeMsg codeMsg;

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
    }
    public class ResendMailResponse
    {
        public ResendMailResponse()
        {
            codeMsg = new CodeMsg();
        }

        private CodeMsg codeMsg;

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
    }
    public class MobileRealTimeResponse
    {
        public MobileRealTimeResponse()
        {
            codeMsg = new CodeMsg();
            dataList = new List<DataListInfoByMobile>();
        }

        private CodeMsg codeMsg;
        private List<DataListInfoByMobile> dataList;

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
        public List<DataListInfoByMobile> DataList { get { return dataList; } set { dataList = value; } }
    }
    public class MailRealTimeResponse
    {
        public MailRealTimeResponse()
        {
            codeMsg = new CodeMsg();
            dataList = new List<DataListInfoByMail>();
        }

        private CodeMsg codeMsg;
        private List<DataListInfoByMail> dataList;

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
        public List<DataListInfoByMail> DataList { get { return dataList; } set { dataList = value; } }
    }
    public class CtqyMultiDataClass
    {
        public CtqyMultiDataClass()
        {
            cardTxns = new List<CardsTxnAll>();
        }

        private string _code;

        public string code
        {
            get { return _code; }
            set { _code = value; }
        }
        private string _msg;

        public string msg
        {
            get { return _msg; }
            set { _msg = value; }
        }
        private string _total;

        public string total
        {
            get { return _total; }
            set { _total = value; }
        }
        private List<CardsTxnAll> _cardTxns;

        public List<CardsTxnAll> cardTxns
        {
            get { return _cardTxns; }
            set { _cardTxns = value; }
        }
    }
    public class CtqyMultiCardBean
    {
        public CtqyMultiCardBean()
        {

        }

        private string _merchantNo;

        public string merchantNo
        {
            get { return _merchantNo; }
            set { _merchantNo = value; }
        }
        private string _userId;

        public string userId
        {
            get { return _userId; }
            set { _userId = value; }
        }
        private List<CtqyMultiCardInfo> _cardInfos;

        public List<CtqyMultiCardInfo> cardInfos
        {
            get { return _cardInfos; }
            set { _cardInfos = value; }
        }
        private string _isVerifyPassword;

        public string isVerifyPassword
        {
            get { return _isVerifyPassword; }
            set { _isVerifyPassword = value; }
        }
        private string _queryType;

        public string queryType
        {
            get { return _queryType; }
            set { _queryType = value; }
        }
        private string _dateFrom;

        public string dateFrom
        {
            get { return _dateFrom; }
            set { _dateFrom = value; }
        }
        private string _dateTo;

        public string dateTo
        {
            get { return _dateTo; }
            set { _dateTo = value; }
        }
        private string _isPager;

        public string isPager
        {
            get { return _isPager; }
            set { _isPager = value; }
        }
        private string _pageNo;

        public string pageNo
        {
            get { return _pageNo; }
            set { _pageNo = value; }
        }
        private string _pageSize;

        public string pageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }
        //private string[] _txnType;

        //public string[] txnType
        //{
        //    get { return _txnType; }
        //    set { _txnType = value; }
        //}
        //private string _billNo;

        //public string billNo
        //{
        //    get { return _billNo; }
        //    set { _billNo = value; }
        //}
        //private string _txnId;

        //public string txnId
        //{
        //    get { return _txnId; }
        //    set { _txnId = value; }
        //}
        private string _sortRule;

        public string sortRule
        {
            get { return _sortRule; }
            set { _sortRule = value; }
        }
    }
    public class CtqyMultiCardInfo
    {
        public CtqyMultiCardInfo()
        {

        }

        private string _cardNo;

        public string cardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; }
        }
        private string _passwordType;

        public string passwordType
        {
            get { return _passwordType; }
            set { _passwordType = value; }
        }
        private string _password;

        public string password
        {
            get { return _password; }
            set { _password = value; }
        }
    }
    public class CardsTxnAll
    {
        private string _txnId;

        public string txnId
        {
            get { return _txnId; }
            set { _txnId = value; }
        }
        private string _cardNo;

        public string cardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; }
        }
        private string _inputChannel;

        public string inputChannel
        {
            get { return _inputChannel; }
            set { _inputChannel = value; }
        }
        private string _inputChannelName;

        public string inputChannelName
        {
            get { return _inputChannelName; }
            set { _inputChannelName = value; }
        }
        private string _txnCode;

        public string txnCode
        {
            get { return _txnCode; }
            set { _txnCode = value; }
        }
        private string _txnName;

        public string txnName
        {
            get { return _txnName; }
            set { _txnName = value; }
        }
        private string _earnPoints;

        public string earnPoints
        {
            get { return _earnPoints; }
            set { _earnPoints = value; }
        }
        private string _earnAmount;

        public string earnAmount
        {
            get { return _earnAmount; }
            set { _earnAmount = value; }
        }
        private string _redeemPoints;

        public string redeemPoints
        {
            get { return _redeemPoints; }
            set { _redeemPoints = value; }
        }
        private string _redeemAmount;

        public string redeemAmount
        {
            get { return _redeemAmount; }
            set { _redeemAmount = value; }
        }
        private string _transferPoints;

        public string transferPoints
        {
            get { return _transferPoints; }
            set { _transferPoints = value; }
        }
        private string _transferAmount;

        public string transferAmount
        {
            get { return _transferAmount; }
            set { _transferAmount = value; }
        }
        private string _adjustPoints;

        public string adjustPoints
        {
            get { return _adjustPoints; }
            set { _adjustPoints = value; }
        }
        private string _adjustAmount;

        public string adjustAmount
        {
            get { return _adjustAmount; }
            set { _adjustAmount = value; }
        }
        private string _upAmount;

        public string upAmount
        {
            get { return _upAmount; }
            set { _upAmount = value; }
        }
        private string _upPoints;

        public string upPoints
        {
            get { return _upPoints; }
            set { _upPoints = value; }
        }
        private string _downAmount;

        public string downAmount
        {
            get { return _downAmount; }
            set { _downAmount = value; }
        }
        private string _downPoints;

        public string downPoints
        {
            get { return _downPoints; }
            set { _downPoints = value; }
        }
        private string _txnPoints;

        public string txnPoints
        {
            get { return _txnPoints; }
            set { _txnPoints = value; }
        }
        private string _txnAmount;

        public string txnAmount
        {
            get { return _txnAmount; }
            set { _txnAmount = value; }
        }
        private string _txnDate;

        public string txnDate
        {
            get { return _txnDate; }
            set { _txnDate = value; }
        }
        private string _txnTime;

        public string txnTime
        {
            get { return _txnTime; }
            set { _txnTime = value; }
        }
        private string _corporateNo;

        public string corporateNo
        {
            get { return _corporateNo; }
            set { _corporateNo = value; }
        }
        private string _corporateName;

        public string corporateName
        {
            get { return _corporateName; }
            set { _corporateName = value; }
        }
        private string _merchantNo;

        public string merchantNo
        {
            get { return _merchantNo; }
            set { _merchantNo = value; }
        }
        private string _merchantName;

        public string merchantName
        {
            get { return _merchantName; }
            set { _merchantName = value; }
        }
        private string _termNo;

        public string termNo
        {
            get { return _termNo; }
            set { _termNo = value; }
        }
        private string _balanceAmount;

        public string balanceAmount
        {
            get { return _balanceAmount; }
            set { _balanceAmount = value; }
        }
        private string _balancePoints;

        public string balancePoints
        {
            get { return _balancePoints; }
            set { _balancePoints = value; }
        }
        private string _remarks;

        public string remarks
        {
            get { return _remarks; }
            set { _remarks = value; }
        }
        private string _rrn;

        public string rrn
        {
            get { return _rrn; }
            set { _rrn = value; }
        }
    }
}