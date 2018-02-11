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
using System.Xml;
using System.Security.Cryptography;
using System.Data.SqlClient;
using System.Configuration;

using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace InternetSales
{
    /// <summary>
    /// Service1 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class Service : System.Web.Services.WebService
    {
        private String strQueryExtractingCodeInfo = System.Configuration.ConfigurationManager.AppSettings["strQueryExtractingCodeInfo"];
        private String strSetExtractingCodeStatus = System.Configuration.ConfigurationManager.AppSettings["strSetExtractingCodeStatus"];
        private String strQueryOrderAllInfo = System.Configuration.ConfigurationManager.AppSettings["strQueryOrderAllInfo"];
        private String strPrivateKeyTraining = "Jh76krkj5JghdOnx9";
        //private String strPrivateKeyProducing = "12hhS88styq2dejbga0616jilindian";
        private String strPrivateKeyProducing = "Jh76krkj5JghdOnx9";

        private String strSqlConn_ActivateCard = System.Configuration.ConfigurationManager.ConnectionStrings["SqlServer_ActivateCard"].ConnectionString ;
        private String strSqlConn_ShoppingCard = System.Configuration.ConfigurationManager.ConnectionStrings["SqlServer_ShoppingCard"].ConnectionString ;

        private Uri address;

        private String strUseProxy = System.Configuration.ConfigurationManager.AppSettings["UseProxy"];
        private String strIsTraining = System.Configuration.ConfigurationManager.AppSettings["IsTraining"];
  
        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        public bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            //直接确认，否则打不开   
            return true;
        }

        //获取Key
        private String getPrivateKey()
        {
            if (strIsTraining.Equals("1"))
                return strPrivateKeyTraining;
            else
                return strPrivateKeyProducing;
        }
        [WebMethod]
        //兑换码查询
        public ExtractingCodeInfoResponse QueryExtractingCodeInfo(ExtractingCodeInfo CClass)
        //public ExtractingCodeInfoResponse QueryExtractingCodeInfo()
        {
            try
            {
                //?issuerId=C000&mobilePhone=13641804277&extractingCode=1415968525638182270&mac=0CCA0BA5AE152466D0ABF01DE0278F84&type=json
                string issuerId = CClass.IssuerId;
                string mobilePhone = CClass.MobilePhone;
                string extractingCode = CClass.ExtractingCode;
                string mac = getMd5Hash(CClass.IssuerId + CClass.MobilePhone + CClass.ExtractingCode + getPrivateKey());
                string type = "xml";
                //string issuerId = "C000";
                //string mobilePhone = "13641804277";
                //string extractingCode = "1415968525638182270";
                //string mac = getMd5Hash(issuerId + mobilePhone + extractingCode + strPrivateKey);
                //mac = "0CCA0BA5AE152466D0ABF01DE0278F84";
                //string type = "xml";

                StringBuilder data = new StringBuilder();
                data.Append(strQueryExtractingCodeInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&mobilePhone=" + mobilePhone);
                data.Append("&extractingCode=" + extractingCode);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);
                // Create the web request
                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    XmlTextReader xmlReader = new XmlTextReader(reader);
                    DataSet xmlDS = new DataSet();
                    xmlDS.ReadXml(xmlReader);

                    ExtractingCodeInfoResponse ecir = new ExtractingCodeInfoResponse();

                    DataTable dtWeChatData = xmlDS.Tables["weChatData"];
                    if (dtWeChatData == null) { throw new Exception("银商无weChatData返回信息。"); }
                    ecir.CodeMsg.Code = dtWeChatData.Rows[0]["code"].ToString();
                    ecir.CodeMsg.Msg = dtWeChatData.Rows[0]["msg"].ToString();

                    if (ecir.CodeMsg.Code == "00") //查询成功
                    {
                        DataRow drData;
                        DataTable dtExtractingCodeInfoData = xmlDS.Tables["extractingCodeInfoData"];
                        if (dtExtractingCodeInfoData == null) { throw new Exception("银商无extractingCodeInfoData返回信息。"); }
                        drData = dtExtractingCodeInfoData.Rows[0];
                        for (int iCol = 0; iCol < dtExtractingCodeInfoData.Columns.Count; iCol++)
                        {
                            if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "extractingCode") { ecir.ExtractingCodeInfoData.ExtractingCode = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "status") { ecir.ExtractingCodeInfoData.Status = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "hotReason") { ecir.ExtractingCodeInfoData.HotReason = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "issuerId") { ecir.ExtractingCodeInfoData.IssuerId = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billNo") { ecir.ExtractingCodeInfoData.BillNo = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billTotalAmount") { ecir.ExtractingCodeInfoData.BillTotalAmount = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billPayTotalAmount") { ecir.ExtractingCodeInfoData.BillPayTotalAmount = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billTotalNum") { ecir.ExtractingCodeInfoData.BillTotalNum = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billPayStatus") { ecir.ExtractingCodeInfoData.BillPayStatus = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billType") { ecir.ExtractingCodeInfoData.BillType = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billCreateTimestamp") { ecir.ExtractingCodeInfoData.BillCreateTimestamp = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billBuyChannel") { ecir.ExtractingCodeInfoData.BillBuyChannel = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "buyerMobilePhone") { ecir.ExtractingCodeInfoData.BuyerMobilePhone = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "holderMobilePhone") { ecir.ExtractingCodeInfoData.HolderMobilePhone = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "createTime") { ecir.ExtractingCodeInfoData.CreateTime = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "updateTime") { ecir.ExtractingCodeInfoData.UpdateTime = drData[iCol].ToString(); }
                        }
                        DataTable dtExtractingCodeInfoDetailDataArray = xmlDS.Tables["extractingCodeInfoDetailDataArray"];
                        if (dtExtractingCodeInfoDetailDataArray == null) { throw new Exception("银商无extractingCodeInfoDetailDataArray返回信息。"); }
                        drData = dtExtractingCodeInfoDetailDataArray.Rows[0];
                        for (int iCol = 0; iCol < dtExtractingCodeInfoDetailDataArray.Columns.Count; iCol++)
                        {
                            if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "productId") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.ProductId = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "areaCode") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.AreaCode = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "productName") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.ProductName = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "status") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.Status = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "cardPrice") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.CardPrice = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "productNum") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.ProductNum = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "payAmount") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.PayAmount = drData[iCol].ToString(); }
                        }
                    }
                    else
                    {
                        Log2SQL(0, "QueryExtractingCodeInfo", "查询提取码失败" + ecir.CodeMsg.Code + ecir.CodeMsg.Msg , address.AbsoluteUri);
                    }

                    return ecir;
                }
            }catch(Exception e)
            {
                Log2SQL(0, "QueryExtractingCodeInfo", "查询提取码失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        [WebMethod]
        //兑换码查询Test
        public ExtractingCodeInfoResponse QueryExtractingCodeInfoTest(String sIssuerId,String sMobilePhone,String sExtractingCode)
        {
            try
            {
                //string issuerId = Decrypt(sIssuerId, "5rdx*IK<");
                //string mobilePhone = Decrypt(sMobilePhone, "5rdx*IK<");
                //string extractingCode = Decrypt(sExtractingCode, "5rdx*IK<");
                string issuerId = sIssuerId;
                string mobilePhone = sMobilePhone;
                string extractingCode = sExtractingCode;
                string mac = getMd5Hash(issuerId + mobilePhone + extractingCode + getPrivateKey());
                string type = "xml";

                StringBuilder data = new StringBuilder();
                data.Append(strQueryExtractingCodeInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&mobilePhone=" + mobilePhone);
                data.Append("&extractingCode=" + extractingCode);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);
                // Create the web request
                address = new Uri(data.ToString());

                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);

                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    XmlTextReader xmlReader = new XmlTextReader(reader);
                    DataSet xmlDS = new DataSet();
                    xmlDS.ReadXml(xmlReader);

                    ExtractingCodeInfoResponse ecir = new ExtractingCodeInfoResponse();

                    DataTable dtWeChatData = xmlDS.Tables["weChatData"];
                    if (dtWeChatData == null) { throw new Exception("银商无weChatData返回信息。"); }
                    ecir.CodeMsg.Code = dtWeChatData.Rows[0]["code"].ToString();
                    ecir.CodeMsg.Msg = dtWeChatData.Rows[0]["msg"].ToString();

                    if (ecir.CodeMsg.Code == "00") //查询成功
                    {
                        DataRow drData;
                        DataTable dtExtractingCodeInfoData = xmlDS.Tables["extractingCodeInfoData"];
                        if (dtExtractingCodeInfoData == null) { throw new Exception("银商无extractingCodeInfoData返回信息。"); }
                        drData = dtExtractingCodeInfoData.Rows[0];
                        for (int iCol = 0; iCol < dtExtractingCodeInfoData.Columns.Count; iCol++)
                        {
                            if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "extractingCode") { ecir.ExtractingCodeInfoData.ExtractingCode = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "status") { ecir.ExtractingCodeInfoData.Status = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "hotReason") { ecir.ExtractingCodeInfoData.HotReason = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "issuerId") { ecir.ExtractingCodeInfoData.IssuerId = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billNo") { ecir.ExtractingCodeInfoData.BillNo = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billTotalAmount") { ecir.ExtractingCodeInfoData.BillTotalAmount = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billPayTotalAmount") { ecir.ExtractingCodeInfoData.BillPayTotalAmount = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billTotalNum") { ecir.ExtractingCodeInfoData.BillTotalNum = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billPayStatus") { ecir.ExtractingCodeInfoData.BillPayStatus = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billType") { ecir.ExtractingCodeInfoData.BillType = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billCreateTimestamp") { ecir.ExtractingCodeInfoData.BillCreateTimestamp = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "billBuyChannel") { ecir.ExtractingCodeInfoData.BillBuyChannel = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "buyerMobilePhone") { ecir.ExtractingCodeInfoData.BuyerMobilePhone = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "holderMobilePhone") { ecir.ExtractingCodeInfoData.HolderMobilePhone = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "createTime") { ecir.ExtractingCodeInfoData.CreateTime = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoData.Columns[iCol].ColumnName == "updateTime") { ecir.ExtractingCodeInfoData.UpdateTime = drData[iCol].ToString(); }
                        }
                        DataTable dtExtractingCodeInfoDetailDataArray = xmlDS.Tables["extractingCodeInfoDetailDataArray"];
                        if (dtExtractingCodeInfoDetailDataArray == null) { throw new Exception("银商无extractingCodeInfoDetailDataArray返回信息。"); }
                        drData = dtExtractingCodeInfoDetailDataArray.Rows[0];
                        for (int iCol = 0; iCol < dtExtractingCodeInfoDetailDataArray.Columns.Count; iCol++)
                        {
                            if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "productId") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.ProductId = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "areaCode") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.AreaCode = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "productName") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.ProductName = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "status") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.Status = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "cardPrice") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.CardPrice = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "productNum") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.ProductNum = drData[iCol].ToString(); }
                            else if (dtExtractingCodeInfoDetailDataArray.Columns[iCol].ColumnName == "payAmount") { ecir.ExtractingCodeInfoData.ExtractingCodeInfoDetailData.PayAmount = drData[iCol].ToString(); }
                        }
                    }

                    return ecir;
                }
            }
            catch (Exception e)
            {
                ExtractingCodeInfoResponse ecir = new ExtractingCodeInfoResponse();
                ecir.CodeMsg.Code = "";
                ecir.CodeMsg.Msg = e.Message;
                return ecir;
            }
        }

        [WebMethod]
        ///提取码状态置为领用状态
        public CodeMsg SetExtractingCodeStatus(SetExtractingCodeStatus CClass, GuIDClass GClass)
        //public CodeMsg SetExtractingCodeStatus()
        {
            try
            {
                if (Iflogin(GClass))
                {
                    //?issuerId=C000&mobilePhone=13641804277&extractingCode=1415968525638182270&cards=2336335500000023318&mac=3B0FE02C83B13F36AB21A4A7768944B3&type=json
                    string issuerId = CClass.IssuerId;
                    string mobilePhone = CClass.MobilePhone;
                    string extractingCode = CClass.ExtractingCode;
                    string cards = CClass.Cards;
                    string mac = getMd5Hash(CClass.IssuerId + CClass.MobilePhone + CClass.ExtractingCode + CClass.Cards + getPrivateKey());
                    string type = "xml";
                    //string issuerId = "C000";
                    //string mobilePhone = "13641804277";
                    //string extractingCode = "1415968525638182270";
                    //string cards = "2336335500000023318";
                    //string mac = getMd5Hash(issuerId + mobilePhone + extractingCode + cards + strPrivateKey );
                    //mac = "3B0FE02C83B13F36AB21A4A7768944B3";
                    //string type = "xml";

                    StringBuilder data = new StringBuilder();
                    data.Append(strSetExtractingCodeStatus);
                    data.Append("?issuerId=" + issuerId);
                    data.Append("&mobilePhone=" + mobilePhone);
                    data.Append("&extractingCode=" + extractingCode);
                    data.Append("&cards=" + cards);
                    data.Append("&mac=" + mac);
                    data.Append("&type=" + type);
                    // Create the web request
                    address = new Uri(data.ToString());
                    HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                    if (strUseProxy == "1")
                    {
                        request.UseDefaultCredentials = true;
                        request.Proxy = getProxy();
                    }
                    Log2SQL(0, "SetExtractingCodeStatus", "开始设置提取码状态"+GClass.GuID, address.AbsoluteUri);
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        XmlTextReader xmlReader = new XmlTextReader(reader);
                        DataSet xmlDS = new DataSet();
                        xmlDS.ReadXml(xmlReader);

                        CodeMsg cm = new CodeMsg();

                        DataTable dtWeChatData = xmlDS.Tables["weChatData"];
                        if (dtWeChatData == null) { throw new Exception("银商无weChatData返回信息。"); }
                        cm.Code = dtWeChatData.Rows[0]["code"].ToString();
                        cm.Msg = dtWeChatData.Rows[0]["msg"].ToString();

                        Log2SQL(0, "SetExtractingCodeStatus", "结束设置提取码状态" + GClass.GuID, cm.Code + cm.Msg );

                        return cm;
                    }
                }
                else
                {
                    Log2SQL(0, "SetExtractingCodeStatus", "开始设置提取码状态,没有登录系统不能进行CUL操作！" + GClass.GuID, address.AbsoluteUri);
                    throw new Exception("没有登录系统不能进行CUL特殊操作！");
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "SetExtractingCodeStatus", "设置提取码状态失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        [WebMethod]
        //卡商城订单查询
        public OrderAllInfoResponse QueryOrderAllInfo(OrderAllInfo CClass)
        //public OrderAllInfoResponse QueryOrderAllInfo()
        {
            try
            {
                //?issuerId=C000&billNo=1416293272466665811&mac=F31A7CE1B7442462E86069C35B53A290&type=xml
                string issuerId = CClass.IssuerId;
                string billNo = CClass.BillNo;
                string mac = getMd5Hash(CClass.IssuerId + CClass.BillNo + getPrivateKey());
                string type = "xml";
                //string issuerId = "C000";
                //string billNo = "1416293272466665811";
                //string mac = getMd5Hash(issuerId + billNo + strPrivateKey);
                //mac = "F31A7CE1B7442462E86069C35B53A290";
                //string type = "xml";

                StringBuilder data = new StringBuilder();
                data.Append(strQueryOrderAllInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&billNo=" + billNo);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);
                // Create the web request
                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    XmlTextReader xmlReader = new XmlTextReader(reader);
                    DataSet xmlDS = new DataSet();
                    xmlDS.ReadXml(xmlReader);

                    OrderAllInfoResponse oair = new OrderAllInfoResponse(0);
                    DataTable dtWeChatData = xmlDS.Tables["weChatData"];
                    if (dtWeChatData == null) { throw new Exception("银商无weChatData返回信息。"); }
                    oair.CodeMsg.Code = dtWeChatData.Rows[0]["code"].ToString();
                    oair.CodeMsg.Msg = dtWeChatData.Rows[0]["msg"].ToString();

                    if (oair.CodeMsg.Code == "00") //查询成功
                    {
                        DataRow drData;
                        DataTable dtCardTransInfoArray = xmlDS.Tables["cardTransInfoArray"];
                        if (dtCardTransInfoArray == null) { throw new Exception("银商无cardTransInfoArray返回信息。"); }
                        int iCount = dtCardTransInfoArray.Rows.Count;
                        oair = new OrderAllInfoResponse(iCount);
                        oair.CodeMsg.Code = dtWeChatData.Rows[0]["code"].ToString();
                        oair.CodeMsg.Msg = dtWeChatData.Rows[0]["msg"].ToString();

                        DataTable dtOrderAllInfoData = xmlDS.Tables["orderAllInfoData"];
                        if (dtOrderAllInfoData == null) { throw new Exception("银商无orderAllInfoData返回信息。"); }
                        drData = dtOrderAllInfoData.Rows[0];
                        for (int iCol = 0; iCol < dtOrderAllInfoData.Columns.Count; iCol++)
                        {
                            if (dtOrderAllInfoData.Columns[iCol].ColumnName == "issuerId") { oair.OrderAllInfoData.IssuerId = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billNo") { oair.OrderAllInfoData.BillNo = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billTotalAmount") { oair.OrderAllInfoData.BillTotalAmount = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billPayTotalAmount") { oair.OrderAllInfoData.BillPayTotalAmount = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billTotalNum") { oair.OrderAllInfoData.BillTotalNum = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billPayStatus") { oair.OrderAllInfoData.BillPayStatus = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billType") { oair.OrderAllInfoData.BillType = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billCreateTimestamp") { oair.OrderAllInfoData.BillCreateTimestamp = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billBuyChannel") { oair.OrderAllInfoData.BillBuyChannel = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "buyerMobilePhone") { oair.OrderAllInfoData.BuyerMobilePhone = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "holderMobilePhone") { oair.OrderAllInfoData.HolderMobilePhone = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "createTime") { oair.OrderAllInfoData.CreateTime = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "updateTime") { oair.OrderAllInfoData.UpdateTime = drData[iCol].ToString(); }
                        }

                        CardTransInfo myCardTrannsInfo;
                        for (int iRow = 0; iRow < iCount; iRow++)
                        {
                            drData = dtCardTransInfoArray.Rows[iRow];
                            myCardTrannsInfo = new CardTransInfo();
                            for (int iCol = 0; iCol < dtCardTransInfoArray.Columns.Count; iCol++)
                            {
                                if (dtCardTransInfoArray.Columns[iCol].ColumnName == "cardNo") { myCardTrannsInfo.CardNo = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "cardHolderMobile") { myCardTrannsInfo.CardHolderMobile = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "amount") { myCardTrannsInfo.Amount = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "payAmount") { myCardTrannsInfo.PayAmount = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "areaCode") { myCardTrannsInfo.AreaCode = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "productId") { myCardTrannsInfo.ProductId = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "productName") { myCardTrannsInfo.ProductName = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "productNum") { myCardTrannsInfo.ProductNum = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "promotionAmount") { myCardTrannsInfo.PromotionAmount = drData[iCol].ToString(); }
                            }
                            oair.OrderAllInfoData.CardTransInfoArray.SetValue(myCardTrannsInfo,iRow);
                        }
                    }
                    else
                    {
                        Log2SQL(0, "QueryOrderAllInfo", "查询订单失败" + oair.CodeMsg.Code + oair.CodeMsg.Msg, address.AbsoluteUri);
                    }


                    return oair;
                }
            }
            catch (Exception e)
            {
                Log2SQL(0, "QueryOrderAllInfo", "查询订单失败" + e.Message, address.AbsoluteUri);
                throw e;
            }
        }

        [WebMethod]
        //卡商城订单查询Test
        public OrderAllInfoResponse QueryOrderAllInfoTest(String sIssuerId,String sBillNo)
        {
            try
            {
                //string issuerId = Decrypt(sIssuerId, "5rdx*IK<");
                //string billNo = Decrypt(sBillNo, "5rdx*IK<");
                string issuerId = sIssuerId;
                string billNo = sBillNo;
                string mac = getMd5Hash(issuerId + billNo + getPrivateKey());
                string type = "xml";

                StringBuilder data = new StringBuilder();
                data.Append(strQueryOrderAllInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&billNo=" + billNo);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);
                // Create the web request
                address = new Uri(data.ToString());

                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);

                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    XmlTextReader xmlReader = new XmlTextReader(reader);
                    DataSet xmlDS = new DataSet();
                    xmlDS.ReadXml(xmlReader);

                    OrderAllInfoResponse oair = new OrderAllInfoResponse(0);
                    DataTable dtWeChatData = xmlDS.Tables["weChatData"];
                    if (dtWeChatData == null) { throw new Exception("银商无weChatData返回信息。"); }
                    oair.CodeMsg.Code = dtWeChatData.Rows[0]["code"].ToString();
                    oair.CodeMsg.Msg = dtWeChatData.Rows[0]["msg"].ToString();

                    if (oair.CodeMsg.Code == "00") //查询成功
                    {
                        DataRow drData;
                        DataTable dtCardTransInfoArray = xmlDS.Tables["cardTransInfoArray"];
                        if (dtCardTransInfoArray == null) { throw new Exception("银商无cardTransInfoArray返回信息。"); }
                        int iCount = dtCardTransInfoArray.Rows.Count;
                        oair = new OrderAllInfoResponse(iCount);
                        oair.CodeMsg.Code = dtWeChatData.Rows[0]["code"].ToString();
                        oair.CodeMsg.Msg = dtWeChatData.Rows[0]["msg"].ToString();

                        DataTable dtOrderAllInfoData = xmlDS.Tables["orderAllInfoData"];
                        if (dtOrderAllInfoData == null) { throw new Exception("银商无orderAllInfoData返回信息。"); }
                        drData = dtOrderAllInfoData.Rows[0];
                        for (int iCol = 0; iCol < dtOrderAllInfoData.Columns.Count; iCol++)
                        {
                            if (dtOrderAllInfoData.Columns[iCol].ColumnName == "issuerId") { oair.OrderAllInfoData.IssuerId = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billNo") { oair.OrderAllInfoData.BillNo = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billTotalAmount") { oair.OrderAllInfoData.BillTotalAmount = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billPayTotalAmount") { oair.OrderAllInfoData.BillPayTotalAmount = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billTotalNum") { oair.OrderAllInfoData.BillTotalNum = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billPayStatus") { oair.OrderAllInfoData.BillPayStatus = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billType") { oair.OrderAllInfoData.BillType = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billCreateTimestamp") { oair.OrderAllInfoData.BillCreateTimestamp = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "billBuyChannel") { oair.OrderAllInfoData.BillBuyChannel = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "buyerMobilePhone") { oair.OrderAllInfoData.BuyerMobilePhone = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "holderMobilePhone") { oair.OrderAllInfoData.HolderMobilePhone = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "createTime") { oair.OrderAllInfoData.CreateTime = drData[iCol].ToString(); }
                            else if (dtOrderAllInfoData.Columns[iCol].ColumnName == "updateTime") { oair.OrderAllInfoData.UpdateTime = drData[iCol].ToString(); }
                        }

                        CardTransInfo myCardTrannsInfo;
                        for (int iRow = 0; iRow < iCount; iRow++)
                        {
                            drData = dtCardTransInfoArray.Rows[iRow];
                            myCardTrannsInfo = new CardTransInfo();
                            for (int iCol = 0; iCol < dtCardTransInfoArray.Columns.Count; iCol++)
                            {
                                if (dtCardTransInfoArray.Columns[iCol].ColumnName == "cardNo") { myCardTrannsInfo.CardNo = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "cardHolderMobile") { myCardTrannsInfo.CardHolderMobile = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "amount") { myCardTrannsInfo.Amount = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "payAmount") { myCardTrannsInfo.PayAmount = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "areaCode") { myCardTrannsInfo.AreaCode = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "productId") { myCardTrannsInfo.ProductId = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "productName") { myCardTrannsInfo.ProductName = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "productNum") { myCardTrannsInfo.ProductNum = drData[iCol].ToString(); }
                                else if (dtCardTransInfoArray.Columns[iCol].ColumnName == "promotionAmount") { myCardTrannsInfo.PromotionAmount = drData[iCol].ToString(); }
                            }
                            oair.OrderAllInfoData.CardTransInfoArray.SetValue(myCardTrannsInfo, iRow);
                        }
                    }

                    return oair;
                }
            }
            catch (Exception e)
            {
                OrderAllInfoResponse oair = new OrderAllInfoResponse(0);
                oair.CodeMsg.Code = "";
                oair.CodeMsg.Msg = e.Message;
                return oair;
            }
        }

        [WebMethod]
        //连接测试
        public OrderAllInfoResponse TestRequest()
        {
            //string sTest = "";
            string sFlag = "";

            try
            {
                string issuerId = "C000";
                string billNo = "1416277035568628903";
                string mac = getMd5Hash(issuerId + billNo + getPrivateKey());
                string type = "xml";

                StringBuilder data = new StringBuilder();
                data.Append(strQueryOrderAllInfo);
                data.Append("?issuerId=" + issuerId);
                data.Append("&billNo=" + billNo);
                data.Append("&mac=" + mac);
                data.Append("&type=" + type);
                // Create the web request
                address = new Uri(data.ToString());
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest;
                if (strUseProxy == "1")
                {
                    request.UseDefaultCredentials = true;
                    request.Proxy = getProxy();
                }

                sFlag = "request.GetResponse";
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();//得到返回结果
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(result);//读取xml格式内容

                    XmlNodeList cardTransInfoArrayList = xmlDoc.SelectNodes("weChatData/orderAllInfoData/cardTransInfoArray");
                    int iCount = cardTransInfoArrayList.Count;
                    OrderAllInfoResponse oair = new OrderAllInfoResponse(iCount);

                    //oair.CodeMsg.Code = xmlDoc.SelectSingleNode("weChatData/code").InnerText;
                    //oair.CodeMsg.Msg = xmlDoc.SelectSingleNode("weChatData/msg").InnerText;

                    //if (oair.CodeMsg.Code == "00") //查询成功
                    //{
                    sFlag = "返回结果缺少字段";

                    oair.OrderAllInfoData.IssuerId = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/issuerId").InnerText;
                    oair.OrderAllInfoData.BillNo = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billNo").InnerText;
                    oair.OrderAllInfoData.BillTotalAmount = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billTotalAmount").InnerText;
                    oair.OrderAllInfoData.BillPayTotalAmount = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billPayTotalAmount").InnerText;
                    oair.OrderAllInfoData.BillTotalNum = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billTotalNum").InnerText;
                    oair.OrderAllInfoData.BillPayStatus = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billPayStatus").InnerText;
                    oair.OrderAllInfoData.BillType = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billType").InnerText;
                    oair.OrderAllInfoData.BillCreateTimestamp = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billCreateTimestamp").InnerText;
                    oair.OrderAllInfoData.BillBuyChannel = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/billBuyChannel").InnerText;
                    oair.OrderAllInfoData.BuyerMobilePhone = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/buyerMobilePhone").InnerText;
                    oair.OrderAllInfoData.HolderMobilePhone = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/holderMobilePhone").InnerText;
                    oair.OrderAllInfoData.CreateTime = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/createTime").InnerText;
                    oair.OrderAllInfoData.UpdateTime = xmlDoc.SelectSingleNode("weChatData/orderAllInfoData/updateTime").InnerText;

                    for (int i = 0; i < iCount; i++)
                    {
                        XmlNode myNode = cardTransInfoArrayList[i];
                        CardTransInfo myCardTrannsInfo = new CardTransInfo();
                        myCardTrannsInfo.CardNo = myNode.SelectSingleNode("cardNo").InnerText;
                        myCardTrannsInfo.CardHolderMobile = myNode.SelectSingleNode("cardHolderMobile").InnerText;
                        myCardTrannsInfo.Amount = myNode.SelectSingleNode("amount").InnerText;
                        myCardTrannsInfo.PayAmount = myNode.SelectSingleNode("payAmount").InnerText;
                        myCardTrannsInfo.AreaCode = myNode.SelectSingleNode("areaCode").InnerText;
                        myCardTrannsInfo.ProductId = myNode.SelectSingleNode("productId").InnerText;
                        myCardTrannsInfo.ProductName = myNode.SelectSingleNode("productName").InnerText;
                        myCardTrannsInfo.ProductNum = myNode.SelectSingleNode("productNum").InnerText;
                        myCardTrannsInfo.PromotionAmount = myNode.SelectSingleNode("promotionAmount").InnerText;

                        oair.OrderAllInfoData.CardTransInfoArray.SetValue(myCardTrannsInfo, i);
                    }

                    //}

                    return oair;
                }
            }
            catch (Exception e)
            {
                OrderAllInfoResponse oair = new OrderAllInfoResponse(1);
                oair.CodeMsg.Code = "NU";
                oair.CodeMsg.Msg = sFlag;

                return oair;
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
                    sqlComm.CommandText = String.Format("insert into ActivationLogForInternetSales(Date,Level,Mothed,Message,Url) values('{0}','{1}','{2}','{3}','{4}')", DateTime.Now.ToString(), strLevel, strMothed, strMessage, strUrl);
                    sqlComm.ExecuteNonQuery();
                }
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
    }

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
    //兑换码查询输入
    public class ExtractingCodeInfo
    {
        public ExtractingCodeInfo() { }

        private string issuerId; //发卡机构号（银商分配）
        private string mobilePhone; // 持有人手机号
        private string extractingCode; //卡片提取码
        private string mac; //请求字符串校验 mac 值, Mac 加密顺序：issuerId, mobilePhone, extractingCode, privateKey
        private string type; //请求数据类型(xml,json)

        public string IssuerId { get { return issuerId; } set { issuerId = value; } }
        public string MobilePhone { get { return mobilePhone; } set { mobilePhone = value; } }
        public string ExtractingCode { get { return extractingCode; } set { extractingCode = value; } }
        public string Mac { get { return mac; } set { mac = value; } }
        public string Type { get { return type; } set { type = value; } }
    }
    //兑换码查询返回
    public class ExtractingCodeInfoResponse
    {
        public ExtractingCodeInfoResponse() 
        { 
            codeMsg = new CodeMsg();
            extractingCodeInfoData = new ExtractingCodeInfoData();
        }

        private CodeMsg codeMsg;
        private ExtractingCodeInfoData extractingCodeInfoData;//兑换码信息

        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
        public ExtractingCodeInfoData ExtractingCodeInfoData { get { return extractingCodeInfoData; } set { extractingCodeInfoData = value; } }
    }
    public class ExtractingCodeInfoData
    {
        public ExtractingCodeInfoData() { extractingCodeInfoDetailData = new ExtractingCodeInfoDetailData(); }

        private String extractingCode;      //提取码
        private String status;              //状态(I-已产生，A-已激活，U-已领取，C-已撤销或关闭)
        private String hotReason;           //问题原因（空表示没问题）
        private String issuerId;            //所属商户
        private String billNo;              //卡商城订单号
        private String billTotalAmount;     //卡商城订单总金额
        private String billPayTotalAmount;  //卡商城订单总实付金额
        private String billTotalNum;        //卡商城订单总卡数量
        private String billPayStatus;       //卡商城订单支付状态
        private String billType;            //卡商城订单类型（ENTITY-实体卡，GIFT-礼品卡）
        private String billCreateTimestamp; //卡商城订单创建时间
        private String billBuyChannel;      //卡商城购买渠道（Wechat-微信，Alipay-支付宝）
        private String buyerMobilePhone;    //购买人手机号
        private String holderMobilePhone;   //持有人手机号
        private String createTime;          //支付完成时间
        private String updateTime;          //兑换完成时间（如果提取码兑换）
        private ExtractingCodeInfoDetailData extractingCodeInfoDetailData;//明细数组

        public String ExtractingCode { get { return extractingCode; } set { extractingCode = value; } }
        public String Status { get { return status; } set { status = value; } }
        public String HotReason { get { return hotReason; } set { hotReason = value; } }
        public String IssuerId { get { return issuerId; } set { issuerId = value; } }
        public String BillNo { get { return billNo; } set { billNo = value; } }
        public String BillTotalAmount { get { return billTotalAmount; } set { billTotalAmount = value; } }
        public String BillPayTotalAmount { get { return billPayTotalAmount; } set { billPayTotalAmount = value; } }
        public String BillTotalNum { get { return billTotalNum; } set { billTotalNum = value; } }
        public String BillPayStatus { get { return billPayStatus; } set { billPayStatus = value; } }
        public String BillType { get { return billType; } set { billType = value; } }
        public String BillCreateTimestamp { get { return billCreateTimestamp; } set { billCreateTimestamp = value; } }
        public String BillBuyChannel { get { return billBuyChannel; } set { billBuyChannel = value; } }
        public String BuyerMobilePhone { get { return buyerMobilePhone; } set { buyerMobilePhone = value; } }
        public String HolderMobilePhone { get { return holderMobilePhone; } set { holderMobilePhone = value; } }
        public String CreateTime { get { return createTime; } set { createTime = value; } }
        public String UpdateTime { get { return updateTime; } set { updateTime = value; } }
        public ExtractingCodeInfoDetailData ExtractingCodeInfoDetailData { get { return extractingCodeInfoDetailData; } set { extractingCodeInfoDetailData = value; } }
    }
    public class ExtractingCodeInfoDetailData
    {
        public ExtractingCodeInfoDetailData() { }

        private String productId;   //商品号
        private String areaCode;    //商品所属区域代码
        private String productName; //商品名称
        private String status;      //明细状态（Y-有效）
        private String cardPrice;   //商品面值（分）
        private String productNum;  //商品数量
        private String payAmount;   //单个商品支付金额

        public string ProductId { get { return productId ; } set { productId = value; } }
        public string AreaCode { get { return areaCode; } set { areaCode = value; } }
        public string ProductName { get { return productName ; } set { productName = value; } }
        public string Status { get { return status; } set {status  = value; } }
        public string CardPrice { get { return cardPrice; } set { cardPrice = value; } }
        public string ProductNum { get { return productNum; } set { productNum = value; } }
        public string PayAmount { get { return payAmount; } set { payAmount = value; } }
    }

    //提取码状态置为领用状态输入
    public class SetExtractingCodeStatus
    {
        public SetExtractingCodeStatus() { }

        private string issuerId; //发卡机构号（银商分配）
        private string mobilePhone; // 持有人手机号
        private string extractingCode; //卡片提取码
        private string cards; //提取码所兑换的所有卡片，用“-”做多张卡片的分隔符。卡片数量应该与提起码代表的商品数量一致。
        private string mac; //请求字符串校验 mac 值, Mac 加密顺序：issuerId, mobilePhone, extractingCode, privateKey
        private string type; //请求数据类型(xml,json)

        public string IssuerId { get { return issuerId; } set { issuerId = value; } }
        public string MobilePhone { get { return mobilePhone; } set { mobilePhone = value; } }
        public string ExtractingCode { get { return extractingCode; } set { extractingCode = value; } }
        public string Cards { get { return cards; } set { cards = value; } }
        public string Mac { get { return mac; } set { mac = value; } }
        public string Type { get { return type; } set { type = value; } }
    }

    //卡商城订单查询输入（包括虚拟卡购卡单及卡片充值及连付充值部分）
    public class OrderAllInfo
    {
        public OrderAllInfo() { }

        private string issuerId; //发卡机构号（银商分配）
        private string billNo; // 商户订单号
        private string mac; //请求字符串校验 mac 值, Mac 加密顺序：issuerId, mobilePhone, extractingCode, privateKey
        private string type; //请求数据类型(xml,json)

        public string IssuerId { get { return issuerId; } set { issuerId = value; } }
        public string BillNo { get { return billNo; } set { billNo = value; } }
        public string Mac { get { return mac; } set { mac = value; } }
        public string Type { get { return type; } set { type = value; } }
    }
    //卡商城订单查询返回（包括虚拟卡购卡单及卡片充值及连付充值部分）
    public class OrderAllInfoResponse
    {
        public OrderAllInfoResponse()
        {
            codeMsg = new CodeMsg();
            orderAllInfoData = new OrderAllInfoData();
        }
        public OrderAllInfoResponse(int iCount)
        {
            codeMsg = new CodeMsg();
            orderAllInfoData = new OrderAllInfoData(iCount);
        }
        
        private CodeMsg codeMsg;
        private OrderAllInfoData orderAllInfoData;//卡商城订单信息
        
        public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
        public OrderAllInfoData OrderAllInfoData { get { return orderAllInfoData; } set { orderAllInfoData = value; } }
    }
    public class OrderAllInfoData
    {
        public OrderAllInfoData() { }
        public OrderAllInfoData(int iCount) { CardTransInfoArray = new CardTransInfo[iCount]; }

        private String issuerId;           //所属商户
        private String billNo;             //卡商城订单号
        private String billTotalAmount;    //卡商城订单总金额
        private String billPayTotalAmount; //卡商城订单总实付金额
        private String billTotalNum;       //卡商城订单总卡数量
        private String billPayStatus;      //卡商城订单支付状态
        private String billType;           //卡商城订单类型（FREE-虚拟卡，RECHARGE-充值）
        private String billCreateTimestamp; //卡商城订单创建时间
        private String billBuyChannel;     //卡商城购买渠道（Wechat-微信，Alipay-支付宝）
        private String buyerMobilePhone;   //购买人手机号
        private String holderMobilePhone;  //持有人手机号
        private String createTime;         //支付完成时间
        private String updateTime;         //兑换完成时间
        //private CardTransInfo[] cardTransInfoArray; //卡数组

        public string IssuerId { get { return issuerId; } set { issuerId = value; } }
        public string BillNo { get { return billNo; } set { billNo = value; } }
        public string BillTotalAmount { get { return billTotalAmount; } set { billTotalAmount = value; } }
        public string BillPayTotalAmount { get { return billPayTotalAmount; } set { billPayTotalAmount = value; } }
        public string BillTotalNum { get { return billTotalNum; } set { billTotalNum = value; } }
        public string BillPayStatus { get { return billPayStatus; } set { billPayStatus = value; } }
        public string BillType { get { return billType; } set { billType = value; } }
        public string BillCreateTimestamp { get { return billCreateTimestamp; } set { billCreateTimestamp = value; } }
        public string BillBuyChannel { get { return billBuyChannel; } set { billBuyChannel = value; } }
        public string BuyerMobilePhone { get { return buyerMobilePhone; } set { buyerMobilePhone = value; } }
        public string HolderMobilePhone { get { return holderMobilePhone; } set { holderMobilePhone = value; } }
        public string CreateTime { get { return createTime; } set { createTime = value; } }
        public string UpdateTime { get { return updateTime; } set { updateTime = value; } }
        public CardTransInfo[] CardTransInfoArray;
    }
    public class CardTransInfo
    {
        public CardTransInfo() { }

        private	String cardNo;           //卡号
        private String cardHolderMobile; //持卡人手机号
        private String amount;           //充值金额（单位元）
        private String payAmount;        //购买或充值的实际支付金额
        private String areaCode;         //商品所属区域代码
        private String productId;        //商品id 充值订单没有
        private String productName;      //商品名称 充值订单没有  
        private String productNum;       //数量        目前都是1
        private String promotionAmount;  //优惠金额  目前都是0.0   
 

        public string CardNo { get { return cardNo; } set { cardNo = value; } }
        public string CardHolderMobile { get { return cardHolderMobile; } set { cardHolderMobile = value; } }
        public string Amount { get { return amount; } set { amount = value; } }
        public string PayAmount { get { return payAmount; } set { payAmount = value; } }
        public string AreaCode { get { return areaCode; } set { areaCode = value; } }
        public string ProductId { get { return productId; } set { productId = value; } }
        public string ProductName { get { return productName; } set { productName = value; } }
        public string ProductNum { get { return productNum; } set { productNum = value; } }
        public string PromotionAmount { get { return promotionAmount; } set { promotionAmount = value; } }
    }


}
