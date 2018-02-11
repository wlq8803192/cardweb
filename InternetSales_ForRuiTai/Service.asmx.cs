using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Data.SqlClient;
using System.Net.Security;

namespace InternetSales_ForRuiTai
{
    /// <summary>
    /// Service1 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://microsoft.com/webservices/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class Service : System.Web.Services.WebService
    {
        private String strUrl = System.Configuration.ConfigurationManager.AppSettings["strUrl"];

        private String strGroupId = System.Configuration.ConfigurationManager.AppSettings["strGroupId"];
        private String strShopId = System.Configuration.ConfigurationManager.AppSettings["strShopId"];
        private String strPosId = System.Configuration.ConfigurationManager.AppSettings["strPosId"];
        private String strAccount = System.Configuration.ConfigurationManager.AppSettings["strAccount"];
        private String strPasswd = System.Configuration.ConfigurationManager.AppSettings["strPasswd"];
        private String strFlowId = System.Configuration.ConfigurationManager.AppSettings["strFlowId"];

        private String strUseProxy = System.Configuration.ConfigurationManager.AppSettings["UseProxy"];
        private String strIsTraining = System.Configuration.ConfigurationManager.AppSettings["IsTraining"];
        private string strKeyTraining = "123456";
        private string strKeyProducing = "3FDE6BB0541387E4EBDADF7C2FF31123";

        private String strSqlConn_ActivateCard = System.Configuration.ConfigurationManager.ConnectionStrings["SqlServer_ActivateCard"].ConnectionString;
        private String strSqlConn_ShoppingCard = System.Configuration.ConfigurationManager.ConnectionStrings["SqlServer_ShoppingCard"].ConnectionString;

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        #region-接口测试-
        //查询
        [WebMethod]
        public CodeInfoResponse TestQueryCode_proc(String CodePassword)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();

            try
            {
                //构造请求信息
                data.Append("type=querycode&data=");
                data.Append("{\"group_id\":\"" + strGroupId + "\"");
                data.Append(",\"shop_id\":\"" + strShopId + "\"");
                data.Append(",\"pos_id\":\"" + strPosId + "\"");
                data.Append(",\"account\":\"" + strAccount + "\"");
                data.Append(",\"passwd\":\"" + getMd5Hash(strPasswd) + "\"");
                data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                data.Append(",\"serial_no\":\"" + Guid.NewGuid().ToString() + "\"");
                data.Append(",\"code_password\":\"" + CodePassword + "\"");
                data.Append(",\"deal_amount_total\":\"0\"");
                data.Append(",\"deal_date\":\"" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "\"");
                data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(strPasswd), CodePassword, strKeyProducing) + "\"}");

                //发起请求
                Uri uri = new Uri(strUrl);
                WebRequest webRequest = WebRequest.Create(uri);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                if (strUseProxy == "1")
                {
                    webRequest.UseDefaultCredentials = true;
                    webRequest.Proxy = getProxy();
                }
                using (Stream requestStream = webRequest.GetRequestStream())
                {
                    byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                    requestStream.Write(paramBytes, 0, paramBytes.Length);
                }
                //响应
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                WebResponse webResponse = webRequest.GetResponse();
                using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                {
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                    Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                    if (myDic.Count == 0) { throw new Exception("瑞泰无返回。"); }

                    foreach (var item in myDic)
                    {
                        if (item.Key == "rtn_flag") { myCir.CodeMsg.Code = item.Value.ToString(); }
                        else if (item.Key == "rtn_msg") { myCir.CodeMsg.Msg = item.Value.ToString(); }

                        else if (item.Key == "code_password") { myCir.CodePassword = item.Value.ToString(); }
                        else if (item.Key == "code_amount") { myCir.CodeAmount = item.Value.ToString(); }
                        else if (item.Key == "discount_total") { myCir.DiscountTotal = item.Value.ToString(); }
                        else if (item.Key == "flow_id") { myCir.FlowId = item.Value.ToString(); }
                        else if (item.Key == "serial_no") { myCir.SerialNo = item.Value.ToString(); }
                        else if (item.Key == "sponsor") { myCir.Sponsor = item.Value.ToString(); }
                        else if (item.Key == "notes") { myCir.Notes = item.Value.ToString(); }
                        else if (item.Key == "code_type") { myCir.CodeType = item.Value.ToString(); }
                        else if (item.Key == "valid_date") { myCir.ValidDate = item.Value.ToString(); }
                    }

                    if (myCir.CodeMsg.Code == null || myCir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                    if (myCir.CodeMsg.Code == "0")  //查询成功检查数据完整性
                    { 
                        if (myCir.CodeAmount == null) { throw new Exception("瑞泰未返回电子券金额。"); }
                        if (myCir.CodeType == null) { throw new Exception("瑞泰未返回密码类型。"); }
                        if (myCir.ValidDate == null) { throw new Exception("瑞泰未返回电子卡有效期。"); }
                    }
                }
                return myCir;
            }
            catch (Exception ex)
            {
                myCir.CodeMsg.Msg = ex.Message;
                return myCir;
            }
        }
        [WebMethod]
        public CodeInfoResponse TestQueryCode(String CodePassword)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();

            try
            {
                //构造请求信息
                data.Append("type=querycode&data=");
                data.Append("{\"group_id\":\"" + strGroupId + "\"");
                data.Append(",\"shop_id\":\"" + strShopId + "\"");
                data.Append(",\"pos_id\":\"" + strPosId + "\"");
                data.Append(",\"account\":\"" + strAccount + "\"");
                data.Append(",\"passwd\":\"" + getMd5Hash(strPasswd) + "\"");
                data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                data.Append(",\"serial_no\":\"" + Guid.NewGuid().ToString() + "\"");
                data.Append(",\"code_password\":\"" + CodePassword + "\"");
                data.Append(",\"deal_amount_total\":\"0\"");
                data.Append(",\"deal_date\":\"" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "\"");
                data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(strPasswd), CodePassword, strKeyTraining) + "\"}");

                //发起请求
                Uri uri = new Uri(strUrl);
                WebRequest webRequest = WebRequest.Create(uri);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                if (strUseProxy == "1")
                {
                    webRequest.UseDefaultCredentials = true;
                    webRequest.Proxy = getProxy();
                }
                using (Stream requestStream = webRequest.GetRequestStream())
                {
                    byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                    requestStream.Write(paramBytes, 0, paramBytes.Length);
                }
                //响应
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                WebResponse webResponse = webRequest.GetResponse();
                using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                {
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                    Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                    if (myDic.Count == 0) { throw new Exception("瑞泰无返回。"); }

                    foreach (var item in myDic)
                    {
                        if (item.Key == "rtn_flag") { myCir.CodeMsg.Code = item.Value.ToString(); }
                        else if (item.Key == "rtn_msg") { myCir.CodeMsg.Msg = item.Value.ToString(); }

                        else if (item.Key == "code_password") { myCir.CodePassword = item.Value.ToString(); }
                        else if (item.Key == "code_amount") { myCir.CodeAmount = item.Value.ToString(); }
                        else if (item.Key == "discount_total") { myCir.DiscountTotal = item.Value.ToString(); }
                        else if (item.Key == "flow_id") { myCir.FlowId = item.Value.ToString(); }
                        else if (item.Key == "serial_no") { myCir.SerialNo = item.Value.ToString(); }
                        else if (item.Key == "sponsor") { myCir.Sponsor = item.Value.ToString(); }
                        else if (item.Key == "notes") { myCir.Notes = item.Value.ToString(); }
                        else if (item.Key == "code_type") { myCir.CodeType = item.Value.ToString(); }
                        else if (item.Key == "valid_date") { myCir.ValidDate = item.Value.ToString(); }
                    }

                    if (myCir.CodeMsg.Code == null || myCir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                    if (myCir.CodeMsg.Code == "0")  //查询成功检查数据完整性
                    {
                        if (myCir.CodeAmount == null) { throw new Exception("瑞泰未返回电子券金额。"); }
                        if (myCir.CodeType == null) { throw new Exception("瑞泰未返回密码类型。"); }
                        if (myCir.ValidDate == null) { throw new Exception("瑞泰未返回电子卡有效期。"); }
                    }
                }
                return myCir;
            }
            catch (Exception ex)
            {
                myCir.CodeMsg.Msg = ex.Message;
                return myCir;
            }
        }
        //查询确认
        public CodeInfoResponse TestQueryCode_proc(String CodePassword, String SerialNo)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();

            try
            {
                //构造请求信息
                data.Append("type=querycode&data=");
                data.Append("{\"group_id\":\"" + strGroupId + "\"");
                data.Append(",\"shop_id\":\"" + strShopId + "\"");
                data.Append(",\"pos_id\":\"" + strPosId + "\"");
                data.Append(",\"account\":\"" + strAccount + "\"");
                data.Append(",\"passwd\":\"" + getMd5Hash(strPasswd) + "\"");
                data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                data.Append(",\"serial_no\":\"" + SerialNo + "\"");
                data.Append(",\"code_password\":\"" + CodePassword + "\"");
                data.Append(",\"deal_amount_total\":\"0\"");
                data.Append(",\"deal_date\":\"" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "\"");
                data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(strPasswd), CodePassword, strKeyProducing) + "\"}");

                //发起请求
                Uri uri = new Uri(strUrl);
                WebRequest webRequest = WebRequest.Create(uri);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                if (strUseProxy == "1")
                {
                    webRequest.UseDefaultCredentials = true;
                    webRequest.Proxy = getProxy();
                }
                using (Stream requestStream = webRequest.GetRequestStream())
                {
                    byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                    requestStream.Write(paramBytes, 0, paramBytes.Length);
                }
                //响应
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                WebResponse webResponse = webRequest.GetResponse();
                using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                {
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                    Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                    if (myDic.Count == 0) { throw new Exception("瑞泰无返回。"); }

                    foreach (var item in myDic)
                    {
                        if (item.Key == "rtn_flag") { myCir.CodeMsg.Code = item.Value.ToString(); }
                        else if (item.Key == "rtn_msg") { myCir.CodeMsg.Msg = item.Value.ToString(); }

                        else if (item.Key == "code_password") { myCir.CodePassword = item.Value.ToString(); }
                        else if (item.Key == "code_amount") { myCir.CodeAmount = item.Value.ToString(); }
                        else if (item.Key == "discount_total") { myCir.DiscountTotal = item.Value.ToString(); }
                        else if (item.Key == "flow_id") { myCir.FlowId = item.Value.ToString(); }
                        else if (item.Key == "serial_no") { myCir.SerialNo = item.Value.ToString(); }
                        else if (item.Key == "sponsor") { myCir.Sponsor = item.Value.ToString(); }
                        else if (item.Key == "notes") { myCir.Notes = item.Value.ToString(); }
                        else if (item.Key == "code_type") { myCir.CodeType = item.Value.ToString(); }
                        else if (item.Key == "valid_date") { myCir.ValidDate = item.Value.ToString(); }
                    }

                    if (myCir.CodeMsg.Code == null || myCir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                    if (myCir.CodeMsg.Code == "0")  //查询成功检查数据完整性
                    {
                        if (myCir.CodeAmount == null) { throw new Exception("瑞泰未返回电子券金额。"); }
                        if (myCir.CodeType == null) { throw new Exception("瑞泰未返回密码类型。"); }
                        if (myCir.ValidDate == null) { throw new Exception("瑞泰未返回电子卡有效期。"); }
                    }
                }
                return myCir;
            }
            catch (Exception ex)
            {
                myCir.CodeMsg.Msg = ex.Message;
                return myCir;
            }
        }
        public CodeInfoResponse TestQueryCode(String CodePassword, String SerialNo)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();

            try
            {
                //构造请求信息
                data.Append("type=querycode&data=");
                data.Append("{\"group_id\":\"" + strGroupId + "\"");
                data.Append(",\"shop_id\":\"" + strShopId + "\"");
                data.Append(",\"pos_id\":\"" + strPosId + "\"");
                data.Append(",\"account\":\"" + strAccount + "\"");
                data.Append(",\"passwd\":\"" + getMd5Hash(strPasswd) + "\"");
                data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                data.Append(",\"serial_no\":\"" + SerialNo + "\"");
                data.Append(",\"code_password\":\"" + CodePassword + "\"");
                data.Append(",\"deal_amount_total\":\"0\"");
                data.Append(",\"deal_date\":\"" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "\"");
                data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(strPasswd), CodePassword, strKeyTraining) + "\"}");

                //发起请求
                Uri uri = new Uri(strUrl);
                WebRequest webRequest = WebRequest.Create(uri);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                if (strUseProxy == "1")
                {
                    webRequest.UseDefaultCredentials = true;
                    webRequest.Proxy = getProxy();
                }
                using (Stream requestStream = webRequest.GetRequestStream())
                {
                    byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                    requestStream.Write(paramBytes, 0, paramBytes.Length);
                }
                //响应
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                WebResponse webResponse = webRequest.GetResponse();
                using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                {
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                    Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                    if (myDic.Count == 0) { throw new Exception("瑞泰无返回。"); }

                    foreach (var item in myDic)
                    {
                        if (item.Key == "rtn_flag") { myCir.CodeMsg.Code = item.Value.ToString(); }
                        else if (item.Key == "rtn_msg") { myCir.CodeMsg.Msg = item.Value.ToString(); }

                        else if (item.Key == "code_password") { myCir.CodePassword = item.Value.ToString(); }
                        else if (item.Key == "code_amount") { myCir.CodeAmount = item.Value.ToString(); }
                        else if (item.Key == "discount_total") { myCir.DiscountTotal = item.Value.ToString(); }
                        else if (item.Key == "flow_id") { myCir.FlowId = item.Value.ToString(); }
                        else if (item.Key == "serial_no") { myCir.SerialNo = item.Value.ToString(); }
                        else if (item.Key == "sponsor") { myCir.Sponsor = item.Value.ToString(); }
                        else if (item.Key == "notes") { myCir.Notes = item.Value.ToString(); }
                        else if (item.Key == "code_type") { myCir.CodeType = item.Value.ToString(); }
                        else if (item.Key == "valid_date") { myCir.ValidDate = item.Value.ToString(); }
                    }

                    if (myCir.CodeMsg.Code == null || myCir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                    if (myCir.CodeMsg.Code == "0")  //查询成功检查数据完整性
                    {
                        if (myCir.CodeAmount == null) { throw new Exception("瑞泰未返回电子券金额。"); }
                        if (myCir.CodeType == null) { throw new Exception("瑞泰未返回密码类型。"); }
                        if (myCir.ValidDate == null) { throw new Exception("瑞泰未返回电子卡有效期。"); }
                    }
                }
                return myCir;
            }
            catch (Exception ex)
            {
                myCir.CodeMsg.Msg = ex.Message;
                return myCir;
            }
        }
        [WebMethod]
        public SetCodeInfoResponse TestSetCodeConfirm_proc(String CodePassword)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();
            SetCodeInfoResponse myScir = new SetCodeInfoResponse();
            String strSerialNo = Guid.NewGuid().ToString();

            try
            {
                //先查询
                myCir = TestQueryCode_proc(CodePassword, strSerialNo);
                if (myCir.CodeMsg.Code == "0")
                {
                    //构造请求信息
                    data.Append("type=confirmdeal&data=");
                    data.Append("{\"group_id\":\"" + strGroupId + "\"");
                    data.Append(",\"shop_id\":\"" + strShopId + "\"");
                    data.Append(",\"pos_id\":\"" + strPosId + "\"");
                    data.Append(",\"account\":\"" + strAccount + "\"");
                    data.Append(",\"passwd\":\"" + getMd5Hash(strPasswd) + "\"");
                    data.Append(",\"oper_type\":\"1\"");
                    data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                    data.Append(",\"serial_no\":\"" + strSerialNo + "\"");
                    data.Append(",\"code_password\":\"" + CodePassword + "\"");
                    data.Append(",\"confirm_amount\":\"" + myCir.CodeAmount + "\"");
                    data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(strPasswd), CodePassword, strKeyProducing) + "\"}");

                    //发起请求
                    Uri uri = new Uri(strUrl);
                    WebRequest webRequest = WebRequest.Create(uri);
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Method = "POST";
                    if (strUseProxy == "1")
                    {
                        webRequest.UseDefaultCredentials = true;
                        webRequest.Proxy = getProxy();
                    }
                    using (Stream requestStream = webRequest.GetRequestStream())
                    {
                        byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                        requestStream.Write(paramBytes, 0, paramBytes.Length);
                    }

                    //响应
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    WebResponse webResponse = webRequest.GetResponse();
                    using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                        Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                        if (myDic.Count == 0)
                        {
                            throw new Exception("瑞泰无返回。");
                        }

                        foreach (var item in myDic)
                        {
                            if (item.Key == "rtn_flag") { myScir.CodeMsg.Code = item.Value.ToString(); }
                            else if (item.Key == "rtn_msg") { myScir.CodeMsg.Msg = item.Value.ToString(); }

                            else if (item.Key == "serial_no") { myScir.SerialNo = item.Value.ToString(); }
                            else if (item.Key == "deal_id") { myScir.DealId = item.Value.ToString(); }
                        }

                        if (myScir.CodeMsg.Code == null || myScir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                        if (myScir.CodeMsg.Code == "0")  //确认电子券消费/取消电子券查询 成功,检查返回数据完整性
                        {
                            if (myScir.DealId == null) { throw new Exception("瑞泰未返回交易编号。"); }
                        }
                    }
                }
                else
                {
                    myScir.CodeMsg.Code = myCir.CodeMsg.Code;
                    myScir.CodeMsg.Msg = myCir.CodeMsg.Msg;
                }
                return myScir;
            }
            catch (Exception ex)
            {
                myScir.CodeMsg.Msg = ex.Message;
                return myScir;
            }
        }
        [WebMethod]
        public SetCodeInfoResponse TestSetCodeConfirm(String CodePassword)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();
            SetCodeInfoResponse myScir = new SetCodeInfoResponse();
            String strSerialNo = Guid.NewGuid().ToString(); 

            try
            {
                //先查询
                myCir = TestQueryCode(CodePassword, strSerialNo);
                if (myCir.CodeMsg.Code == "0")
                {
                    //构造请求信息
                    data.Append("type=confirmdeal&data=");
                    data.Append("{\"group_id\":\"" + strGroupId + "\"");
                    data.Append(",\"shop_id\":\"" + strShopId + "\"");
                    data.Append(",\"pos_id\":\"" + strPosId + "\"");
                    data.Append(",\"account\":\"" + strAccount + "\"");
                    data.Append(",\"passwd\":\"" + getMd5Hash(strPasswd) + "\"");
                    data.Append(",\"oper_type\":\"1\"");
                    data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                    data.Append(",\"serial_no\":\"" + strSerialNo + "\"");
                    data.Append(",\"code_password\":\"" + CodePassword + "\"");
                    data.Append(",\"confirm_amount\":\"" + myCir.CodeAmount + "\"");
                    data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(strPasswd), CodePassword, strKeyTraining) + "\"}");

                    //发起请求
                    Uri uri = new Uri(strUrl);
                    WebRequest webRequest = WebRequest.Create(uri);
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Method = "POST";
                    if (strUseProxy == "1")
                    {
                        webRequest.UseDefaultCredentials = true;
                        webRequest.Proxy = getProxy();
                    }
                    using (Stream requestStream = webRequest.GetRequestStream())
                    {
                        byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                        requestStream.Write(paramBytes, 0, paramBytes.Length);
                    }

                    //响应
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    WebResponse webResponse = webRequest.GetResponse();
                    using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                        Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                        if (myDic.Count == 0)
                        {
                            throw new Exception("瑞泰无返回。");
                        }

                        foreach (var item in myDic)
                        {
                            if (item.Key == "rtn_flag") { myScir.CodeMsg.Code = item.Value.ToString(); }
                            else if (item.Key == "rtn_msg") { myScir.CodeMsg.Msg = item.Value.ToString(); }

                            else if (item.Key == "serial_no") { myScir.SerialNo = item.Value.ToString(); }
                            else if (item.Key == "deal_id") { myScir.DealId = item.Value.ToString(); }
                        }

                        if (myScir.CodeMsg.Code == null || myScir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                        if (myScir.CodeMsg.Code == "0")  //确认电子券消费/取消电子券查询 成功,检查返回数据完整性
                        {
                            if (myScir.DealId == null) { throw new Exception("瑞泰未返回交易编号。"); }
                        }
                    }
                }
                else
                {
                    myScir.CodeMsg.Code = myCir.CodeMsg.Code;
                    myScir.CodeMsg.Msg = myCir.CodeMsg.Msg;
                }
                return myScir;
            }
            catch (Exception ex)
            {
                myScir.CodeMsg.Msg = ex.Message;
                return myScir;
            }
        }
        //查询取消
        [WebMethod]
        public SetCodeInfoResponse TestSetCodeCancel(String CodePassword)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();
            SetCodeInfoResponse myScir = new SetCodeInfoResponse();
            String strSerialNo = Guid.NewGuid().ToString();

            try
            {
                //先查询
                myCir = TestQueryCode(CodePassword, strSerialNo);
                if (myCir.CodeMsg.Code == "0")
                {
                    //构造请求信息
                    data.Append("type=canceldeal&data=");
                    data.Append("{\"group_id\":\"" + strGroupId + "\"");
                    data.Append(",\"shop_id\":\"" + strShopId + "\"");
                    data.Append(",\"pos_id\":\"" + strPosId + "\"");
                    data.Append(",\"account\":\"" + strAccount + "\"");
                    data.Append(",\"passwd\":\"" + strPasswd + "\"");
                    data.Append(",\"oper_type\":\"0\"");
                    data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                    data.Append(",\"serial_no\":\"" + strSerialNo + "\"");
                    data.Append(",\"code_password\":\"" + CodePassword + "\"");
                    data.Append(",\"confirm_amount\":\"" + myCir.CodeAmount + "\"");
                    data.Append(",\"sign\":\"" + GetSign(strAccount, strPasswd, CodePassword, strKeyProducing) + "\"}");

                    //发起请求
                    Uri uri = new Uri(strUrl);
                    WebRequest webRequest = WebRequest.Create(uri);
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Method = "POST";
                    if (strUseProxy == "1")
                    {
                        webRequest.UseDefaultCredentials = true;
                        webRequest.Proxy = getProxy();
                    }
                    using (Stream requestStream = webRequest.GetRequestStream())
                    {
                        byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                        requestStream.Write(paramBytes, 0, paramBytes.Length);
                    }

                    //响应
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    WebResponse webResponse = webRequest.GetResponse();
                    using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                        Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                        if (myDic.Count == 0)
                        {
                            throw new Exception("瑞泰无返回。");
                        }

                        foreach (var item in myDic)
                        {
                            if (item.Key == "rtn_flag") { myScir.CodeMsg.Code = item.Value.ToString(); }
                            else if (item.Key == "rtn_msg") { myScir.CodeMsg.Msg = item.Value.ToString(); }

                            else if (item.Key == "serial_no") { myScir.SerialNo = item.Value.ToString(); }
                            else if (item.Key == "deal_id") { myScir.DealId = item.Value.ToString(); }
                        }

                        if (myScir.CodeMsg.Code == null || myScir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                        if (myScir.CodeMsg.Code == "0")  //确认电子券消费/取消电子券查询 成功,检查返回数据完整性
                        {
                            if (myScir.DealId == null) { throw new Exception("瑞泰未返回交易编号。"); }
                        }
                    }
                }
                else
                {
                    myScir.CodeMsg.Code = myCir.CodeMsg.Code;
                    myScir.CodeMsg.Msg = myCir.CodeMsg.Msg;
                }
                return myScir;
            }
            catch (Exception ex)
            {
                myScir.CodeMsg.Msg = ex.Message;
                return myScir;
            }
        }
        //数据库连接测试
        [WebMethod]
        public string TestConnectDatabase_ShoppingCard()
        {
            string sReturn = "";

            using (SqlConnection sqlConn = new SqlConnection(strSqlConn_ShoppingCard))
            {
                using (SqlCommand sqlComm = new SqlCommand())
                {
                    sqlConn.Open();
                    sqlComm.Connection = sqlConn;
                    sqlComm.CommandText = String.Format("SELECT count(*) FROM arealist");
                    SqlDataAdapter sqlDA = new SqlDataAdapter(sqlComm);
                    DataSet ds = new DataSet();
                    sqlDA.Fill(ds);

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];
                        sReturn = dt.Rows[0][0].ToString();
                    }
                }
            }
            return sReturn;
        }
        [WebMethod]
        public string TestConnectDatabase_ActivateCard()
        {
            string sReturn = "";

            using (SqlConnection sqlConn = new SqlConnection(strSqlConn_ActivateCard))
            {
                using (SqlCommand sqlComm = new SqlCommand())
                {
                    sqlConn.Open();
                    sqlComm.Connection = sqlConn;
                    sqlComm.CommandText = String.Format("SELECT count(*) FROM ActivationLogForInternetSales");
                    SqlDataAdapter sqlDA = new SqlDataAdapter(sqlComm);
                    DataSet ds = new DataSet();
                    sqlDA.Fill(ds);

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];
                        sReturn = dt.Rows[0][0].ToString();
                    }
                }
            }
            return sReturn;
        }
        #endregion

        #region-业务调用函数-
        //查询电子券
        [WebMethod]
        public CodeInfoResponse QueryCode(CodeInfo CClass)
        {
            StringBuilder data = new StringBuilder();
            CodeInfoResponse myCir = new CodeInfoResponse();

            try
            {
                //构造请求信息
                data.Append("type=querycode&data=");
                data.Append("{\"group_id\":\"" + strGroupId + "\"");
                data.Append(",\"shop_id\":\"" + CClass.ShopId + "\"");
                data.Append(",\"pos_id\":\"" + strPosId + "\"");
                data.Append(",\"account\":\"" + CClass.Account + "\"");
                data.Append(",\"passwd\":\"" + getMd5Hash(CClass.Passwd) + "\"");
                data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                data.Append(",\"serial_no\":\"" + CClass.SerialNo + "\"");
                data.Append(",\"code_password\":\"" + CClass.CodePassword + "\"");
                data.Append(",\"deal_amount_total\":\"" + CClass.DealAmountTotal + "\"");
                data.Append(",\"deal_date\":\"" + CClass.DealDate + "\"");
                data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(CClass.Passwd), CClass.CodePassword, CClass.Key) + "\"}");

                //发起请求
                Uri uri = new Uri(strUrl);
                WebRequest webRequest = WebRequest.Create(uri);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                if (strUseProxy == "1")
                {
                    webRequest.UseDefaultCredentials = true;
                    webRequest.Proxy = getProxy();
                }
                using (Stream requestStream = webRequest.GetRequestStream())
                {
                    byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                    requestStream.Write(paramBytes, 0, paramBytes.Length);
                }

                //响应
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                WebResponse webResponse = webRequest.GetResponse();
                using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                {
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                    Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                    if (myDic.Count == 0) 
                    { 
                        Log2SQL(0, "QueryCode", "查询电子券失败:瑞泰无返回。", data.ToString());
                        throw new Exception("瑞泰无返回。"); 
                    }

                    foreach (var item in myDic)
                    {
                        if (item.Key == "rtn_flag") { myCir.CodeMsg.Code = item.Value.ToString(); }
                        else if (item.Key == "rtn_msg") { myCir.CodeMsg.Msg = item.Value.ToString(); }

                        else if (item.Key == "code_password") { myCir.CodePassword = item.Value.ToString(); }
                        else if (item.Key == "code_amount") { myCir.CodeAmount = item.Value.ToString(); }
                        else if (item.Key == "discount_total") { myCir.DiscountTotal = item.Value.ToString(); }
                        else if (item.Key == "flow_id") { myCir.FlowId = item.Value.ToString(); }
                        else if (item.Key == "serial_no") { myCir.SerialNo = item.Value.ToString(); }
                        else if (item.Key == "sponsor") { myCir.Sponsor = item.Value.ToString(); }
                        else if (item.Key == "notes") { myCir.Notes = item.Value.ToString(); }
                        else if (item.Key == "code_type") { myCir.CodeType = item.Value.ToString(); }
                        else if (item.Key == "valid_date") { myCir.ValidDate = item.Value.ToString(); }
                    }

                    if (myCir.CodeMsg.Code == null || myCir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                    if (myCir.CodeMsg.Code == "0")  //查询成功,检查返回数据完整性
                    {
                        if (myCir.CodeAmount == null) { throw new Exception("瑞泰未返回电子券金额。"); }
                        if (myCir.CodeType == null) { throw new Exception("瑞泰未返回密码类型。"); }
                        if (myCir.ValidDate == null) { throw new Exception("瑞泰未返回电子卡有效期。"); }
                    }
                }
                return myCir;
            }
            catch(Exception ex)
            {
                Log2SQL(0, "QueryCode", "查询电子券失败:" + ex.Message, data.ToString());
                throw ex;
            }
        }
        //确认电子券消费/取消电子券查询
        [WebMethod]
        public SetCodeInfoResponse SetCode(SetCodeInfo CClass, GuIDClass GClass)
        {
            StringBuilder data = new StringBuilder();
            SetCodeInfoResponse myScir = new SetCodeInfoResponse();

            string strDes;
            if (CClass.OperType == "1")
            {
                strDes = "确认电子券消费";
            }
            else
            {
                strDes = "取消电子券查询";
            }

            try 
            {
                if (Iflogin(GClass))
                {
                    //构造请求信息
                    data.Append("type=confirmdeal&data=");
                    data.Append("{\"group_id\":\"" + strGroupId + "\"");
                    data.Append(",\"shop_id\":\"" + CClass.ShopId + "\"");
                    data.Append(",\"pos_id\":\"" + strPosId + "\"");
                    data.Append(",\"account\":\"" + CClass.Account + "\"");
                    data.Append(",\"passwd\":\"" + getMd5Hash(CClass.Passwd) + "\"");
                    data.Append(",\"oper_type\":\"" + CClass.OperType + "\"");
                    data.Append(",\"flow_id\":\"" + strFlowId + "\"");
                    data.Append(",\"serial_no\":\"" + CClass.SerialNo + "\"");
                    data.Append(",\"code_password\":\"" + CClass.CodePassword + "\"");
                    data.Append(",\"confirm_amount\":\"" + CClass.ConfirmAmount + "\"");
                    data.Append(",\"sign\":\"" + GetSign(strAccount, getMd5Hash(CClass.Passwd), CClass.CodePassword, CClass.Key) + "\"}");

                    //发起请求
                    Uri uri = new Uri(strUrl);
                    WebRequest webRequest = WebRequest.Create(uri);
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Method = "POST";
                    if (strUseProxy == "1")
                    {
                        webRequest.UseDefaultCredentials = true;
                        webRequest.Proxy = getProxy();
                    }
                    using (Stream requestStream = webRequest.GetRequestStream())
                    {
                        byte[] paramBytes = Encoding.UTF8.GetBytes(data.ToString());
                        requestStream.Write(paramBytes, 0, paramBytes.Length);
                    }

                    //响应
                    Log2SQL(0, "SetCode", "开始" + strDes + GClass.GuID, data.ToString());
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    WebResponse webResponse = webRequest.GetResponse();
                    using (StreamReader myStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        object obj = jss.DeserializeObject(myStreamReader.ReadToEnd());
                        Dictionary<string, object> myDic = (Dictionary<string, object>)obj;
                        if (myDic.Count == 0)
                        {
                            Log2SQL(0, "SetCode", strDes + "失败:瑞泰无返回。", data.ToString());
                            throw new Exception("瑞泰无返回。");
                        }

                        foreach (var item in myDic)
                        {
                            if (item.Key == "rtn_flag") { myScir.CodeMsg.Code = item.Value.ToString(); }
                            else if (item.Key == "rtn_msg") { myScir.CodeMsg.Msg = item.Value.ToString(); }

                            else if (item.Key == "serial_no") { myScir.SerialNo = item.Value.ToString(); }
                            else if (item.Key == "deal_id") { myScir.DealId = item.Value.ToString(); }
                        }

                        if (myScir.CodeMsg.Code == null || myScir.CodeMsg.Msg == null) { throw new Exception("瑞泰未返回状态码或状态描述。"); }
                        if (myScir.CodeMsg.Code == "0")  //确认电子券消费/取消电子券查询 成功,检查返回数据完整性
                        {
                            if (myScir.DealId == null) { throw new Exception("瑞泰未返回交易编号。"); }
                        }
                    }

                    Log2SQL(0, "SetCode", "结束" + strDes + GClass.GuID, myScir.CodeMsg.Code + myScir.CodeMsg.Msg);
                    return myScir;
                }
                else
                {
                    Log2SQL(0, "SetCode", "没有登录系统不能进行操作！" + GClass.GuID, data.ToString());
                    throw new Exception("没有登录系统不能进行操作！");
                }
            }
            catch (Exception ex)
            {
                Log2SQL(0, "SetCode", strDes + "失败:" + ex.Message, data.ToString());
                throw ex;
            }
        }
        #endregion

        #region-内部函数-
        //获取签名验证 md5(account+passwd+code_password+密钥) 32位
        private string GetSign(string sAccount, string sPasswd, string sCodePassword,string sKey) 
        {
            try 
            {
                return getMd5Hash(sAccount + sPasswd + sCodePassword + sKey);
            }
            catch 
            {
                return "";
            }
            
        }
        //得到MD5值
        static string getMd5Hash(string input)
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
        //MD5解密：
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
        //验证GuID
        private bool Iflogin(GuIDClass GClass)
        {
            bool bReturn = false;
            try
            {
                if (GClass.GuID.Equals(""))
                { }
                else
                {
                    using (SqlConnection sqlConn = new SqlConnection(strSqlConn_ShoppingCard))
                    {
                        using (SqlCommand sqlComm = new SqlCommand())
                        {
                            sqlConn.Open();
                            sqlComm.Connection = sqlConn;
                            sqlComm.CommandText = String.Format("SELECT *  FROM [ShoppingCard].[dbo].[LoginInfo] where IsLogin=1 and GuID='{0}'", GClass.GuID);
                            SqlDataAdapter sqlDA = new SqlDataAdapter(sqlComm);
                            DataSet ds = new DataSet();
                            sqlDA.Fill(ds);

                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                bReturn = true;
                            }
                        }
                    }
                }

                return bReturn;
            }
            catch
            {
                return bReturn;
            }
        }
        //写数据库日志
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
                    sqlComm.CommandText = String.Format("insert into ActivationLogForInternetSales(Date,Level,Mothed,Message,Url) values('{0}','{1}','{2}','{3}','{4}')", DateTime.Now.ToString(), strLevel, strMothed + "(瑞泰)", strMessage, strUrl);
                    sqlComm.ExecuteNonQuery();
                }
            }
        }
        //获取代理
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
        #endregion
    }

    #region-参数类-
    public class CodeMsg
    {
        public CodeMsg() { }
        private string code; public string Code { get { return code; } set { code = value; } }
        private string msg; public string Msg { get { return msg; } set { msg = value; } }
    }

    //传递GuID验证参数
    public class GuIDClass
    {
        public GuIDClass() { }
        private string guID; public string GuID { get { return guID; } set { guID = value; } }
    }

    //电子券查询输入
    public class CodeInfo
    {
        public CodeInfo() { }

        private string groupId;         //集团编号(rt提供)
        private string shopId;          //门店编号(rt提供)
        private string posId;           //pos机编号(任意填写)
        private string account;         //账号(rt提供)
        private string passwd;          //密码md5 32位(rt提供)
        private string flowId;          //小票号(任意填写)
        private string serialNo;        //流水号(唯一标识,不能重复,如果查询失败,或者消费失败,则产生新的流水号)
        private string codePassword;    //电子券密码
        private string key;             //密钥
        private string dealAmountTotal; //消费总金额,单位分,如果没有订单填0
        private string dealDate;        //交易时间,对账以该时间为准(yyyy-MM-DD hh:mm:ss)
        private string sign;            //签名验证 md5(account+passwd+code_password+密钥) 32位}(密钥rt提供)

        public string GroupId { get { return groupId; } set { groupId = value; } }
        public string ShopId { get { return shopId; } set { shopId = value; } }
        public string PosId { get { return posId; } set { posId = value; } }
        public string Account { get { return account; } set { account = value; } }
        public string Passwd { get { return passwd; } set { passwd = value; } }
        public string FlowId { get { return flowId; } set { flowId = value; } }
        public string SerialNo { get { return serialNo; } set { serialNo = value; } }
        public string CodePassword { get { return codePassword; } set { codePassword = value; } }
        public string Key { get { return key; } set { key = value; } }
        public string DealAmountTotal { get { return dealAmountTotal; } set { dealAmountTotal = value; } }
        public string DealDate { get { return dealDate; } set { dealDate = value; } }
        public string Sign { get { return sign; } set { sign = value; } }
    }

    //电子券查询返回
    public class CodeInfoResponse
    {
        public CodeInfoResponse() 
        {
            codeMsg = new CodeMsg();
        }

        private CodeMsg codeMsg;
        private string codePassword; //电子券密码
        private string codeAmount;   //电子券金额(分)
        private string discountTotal;//优惠总金额(分)
        private string flowId;       //小票号
        private string serialNo;     //流水号
        private string sponsor;      //发行方信息
        private string notes;        //备注信息
        private string codeType;     //密码类型(001,002单次消费,003,004多次消费,007折扣券)
        private string validDate;    //电子卡有效期(yyyy-MM-DD)

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
        public string CodePassword { get { return codePassword; } set { codePassword = value; } }
        public string CodeAmount { get { return codeAmount; } set { codeAmount = value; } }
        public string DiscountTotal { get { return discountTotal; } set { discountTotal = value; } }
        public string FlowId { get { return flowId; } set { flowId = value; } }
        public string SerialNo { get { return serialNo; } set { serialNo = value; } }
        public string Sponsor { get { return sponsor; } set { sponsor = value; } }
        public string Notes { get { return notes; } set { notes = value; } }
        public string CodeType { get { return codeType; } set { codeType = value; } }
        public string ValidDate { get { return validDate; } set { validDate = value; } }
    }

    //电子券消费确认/取消输入
    public class SetCodeInfo
    {
        public SetCodeInfo() { }

        private string groupId;         //集团编号(rt提供)
        private string shopId;          //门店编号(rt提供)
        private string posId;           //pos机编号(任意填写)
        private string account;         //账号(rt提供)
        private string passwd;          //密码md5 32位(rt提供)
        private string operType;        //1确认 ， 0 取消
        private string flowId;          //小票号(任意填写)
        private string serialNo;        //流水号(唯一标识,不能重复,如果查询失败,或者消费失败,则产生新的流水号)
        private string codePassword;    //电子券密码
        private string confirmAmount;   //电子券消费金额 (分)
        private string key;             //密钥
        private string sign;            //签名验证 md5(account+passwd+code_password+密钥) 32位}(密钥rt提供)

        public string GroupId { get { return groupId; } set { groupId = value; } }
        public string ShopId { get { return shopId; } set { shopId = value; } }
        public string PosId { get { return posId; } set { posId = value; } }
        public string Account { get { return account; } set { account = value; } }
        public string Passwd { get { return passwd; } set { passwd = value; } }
        public string OperType { get { return operType; } set { operType = value; } }
        public string FlowId { get { return flowId; } set { flowId = value; } }
        public string SerialNo { get { return serialNo; } set { serialNo = value; } }
        public string CodePassword { get { return codePassword; } set { codePassword = value; } }
        public string ConfirmAmount { get { return confirmAmount; } set { confirmAmount = value; } }
        public string Key { get { return key; } set { key = value; } }
        public string Sign { get { return sign; } set { sign = value; } }
    }

    //电子券消费确认/取消返回
    public class SetCodeInfoResponse
    {
        public SetCodeInfoResponse()
        {
            codeMsg = new CodeMsg();
        }

        private CodeMsg codeMsg;
        private string serialNo; //流水号
        private string dealId;   //瑞泰交易编号

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
        public string SerialNo { get { return serialNo; } set { serialNo = value; } }
        public string DealId { get { return dealId; } set { dealId = value; } }
    }
    #endregion
}
