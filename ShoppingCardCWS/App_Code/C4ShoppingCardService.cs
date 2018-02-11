using System;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Services3.Security.Tokens;
using System.Net;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;
using log4net;
using System.Reflection;

//'modify code 036:
//'date;2014/8/26
//'auther:Hyron bjy 
//'remark:增加三种换卡功能

//'modify code 047:
//'date;2015/5/28
//'auther:Hyron qm 
//'remark:记名卡非记名卡注册接口

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
[WebService(Namespace = "http://ChinaIT.Carrefour.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class C4ShoppingCardService : System.Web.Services.WebService
{
    static string strProxyAddress = "";
    static string strUserName = "";
    static string strPassword = "";
    static string strCertificateAddress = "";
    static string strCertificatePassword = "";
    static string strTokenUserName = "";
    static string strTokenPassword = "";
    static string strCULURL = "";
    static string strCULTestURL = "";
    static ILog log;
    static string strSqlConn = "";
    static string strSqlConn2 = "";
    static string strIsTest = "";

    public C4ShoppingCardService()
    {

        //如果使用设计的组件，请取消注释以下行 
        //InitializeComponent(); 
        //测试加解密
        string strEncrypt = Encrypt("5TAke656", "5rdx*IK<");
        string strDecrpty = Decrypt(strEncrypt, "5rdx*IK<");
        //HttpContext.Current.Request.ServerVariables

        //strEncrypt = Encrypt("gzjlfc300&123", "5rdx*IK<");
        //strDecrpty = Decrypt(strEncrypt, "5rdx*IK<");

        //strEncrypt = Encrypt("testPass", "5rdx*IK<");
        //strDecrpty = Decrypt(strEncrypt, "5rdx*IK<");

        strProxyAddress = GetAppConfig("ProxyAddress");
        strUserName = GetAppConfig("UserName");
        strPassword = GetAppConfig("Password");
        strPassword = Decrypt(strPassword, "5rdx*IK<");

        strCertificateAddress = GetAppConfig("CertificateAddress");
        strCertificatePassword = GetAppConfig("CertificatePassword");
        strCertificatePassword = Decrypt(strCertificatePassword, "5rdx*IK<");

        strTokenUserName = GetAppConfig("TokenUserName");
        strTokenPassword = GetAppConfig("TokenPassword");
        strTokenPassword = Decrypt(strTokenPassword, "5rdx*IK<");

        strSqlConn = GetConnectionStringsConfig("SqlServer");
        strSqlConn2 = GetConnectionStringsConfig("SqlServer2");
        strCULURL = GetAppConfig("CULURL");
        strCULTestURL = GetAppConfig("CULTestURL");
        strIsTest = GetAppConfig("IsTest");

        log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    #region---Just For Test---
    //券信息查询
    [WebMethod]
    public VInfoDataClass vinfoTest()
    {
        try
        {
            ImTxnWebReference.VInfoBean vinfoBean = new ImTxnWebReference.VInfoBean();
            vinfoBean.merchantNo = "102451054110670";
            vinfoBean.userId = "c4p";
            vinfoBean.typeId = "86114050";
            vinfoBean.seqNoFrom = "000000001";
            vinfoBean.seqNoTo = "000000001";
            vinfoBean.voucherNo = "";
            vinfoBean.isPager = "N";
            vinfoBean.pageNo = "1";
            vinfoBean.pageSize = "";


            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.VInfoData vmsg = culService.vinfo(vinfoBean);
            VInfoDataClass idc;
            if (vmsg.codeMsg.code != "01")
            {
                idc = new VInfoDataClass();
                idc.Total = vmsg.total;

                idc.VCodeMsg.Code = vmsg.codeMsg.code;
                idc.VCodeMsg.Msg = vmsg.codeMsg.msg;
                return idc;
            }
            else
            {
                idc = new VInfoDataClass(vmsg.vouchers.Length);
                idc.Total = vmsg.total;

                idc.VCodeMsg.Code = vmsg.codeMsg.code;
                idc.VCodeMsg.Msg = vmsg.codeMsg.msg;
            }


            Int16 i16Count = 0;
            foreach (ImTxnWebReference.Voucher imVoucher in vmsg.vouchers)
            {
                Voucher c4Voucher = new Voucher();
                c4Voucher.VoucherNo = imVoucher.voucherNo;
                c4Voucher.SeqNo = imVoucher.seqNo;
                c4Voucher.TypeId = imVoucher.typeId;
                c4Voucher.Status = imVoucher.status;
                c4Voucher.ExpiredDate = imVoucher.expiredDate;
                c4Voucher.ActivedDMer = imVoucher.activeMer;
                c4Voucher.ActivedDate = imVoucher.activeDate;
                c4Voucher.UseMer = imVoucher.useMer;
                c4Voucher.UseTime = imVoucher.useTime;
                c4Voucher.Amount = imVoucher.amount;
                c4Voucher.ActivedMerName = imVoucher.activeMerName;
                c4Voucher.UseMerName = imVoucher.useMerName;

                //idc.SetValue(c4Card, i16Count);
                idc.Vouchers.SetValue(c4Voucher, i16Count);
                i16Count++;
            }
            return idc;
        }
        catch (Exception e)
        {

            VInfoDataClass idc = new VInfoDataClass();

            idc.VCodeMsg.Code = "NU";
            idc.VCodeMsg.Msg = e.Message.Substring(0, 40);

            return idc;

        }
    }
    #endregion

    [WebMethod]
    public string HelloWorld() 
    {
        log.Info("开始 Hello Word");
        Log2SQL(0, 1, "HelloWorld", "开始 Hello Word");
        return "Hello World";
       
    }
    //取得CUL服务
    private ImTxnWebReference.imTxnServiceWse getCULService()
    {
        if (strCULURL.StartsWith("https", StringComparison.OrdinalIgnoreCase))
        {
            System.Net.ServicePointManager.Expect100Continue = true;
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
        }

        //server certificate validation
        System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(RemoteCertificateValidationCallback);

        //prepare client side certificate
        ImTxnWebReference.imTxnServiceWse service = new ImTxnWebReference.imTxnServiceWse();
        //正式不用测试要用
        if (strIsTest.Equals("1"))
        {
            WebProxy myProxy = new WebProxy(strProxyAddress, true);
            myProxy.Credentials = new NetworkCredential(strUserName, strPassword);
            service.Proxy = myProxy;
            service.Timeout = 1200000;
        }

        X509Certificate clientCert = new X509Certificate2(strCertificateAddress, strCertificatePassword);    //the certificate file and the access password
        service.ClientCertificates.Add(clientCert);

        //set policy and username token
        service.SetPolicy("clientPolicy");  //policy name in 'wse3policyCache.config'
        UsernameToken token = new UsernameToken(strTokenUserName, strTokenPassword, PasswordOption.SendPlainText);  //username and password of token
        service.SetClientCredential(token);
        service.Url = strCULURL;     //endpoint url of service
        return service;
    }
    ////取得CUL Test 服务
    //private ImTxnTestWebReference.imTxnServiceWse getCULTestService()
    //{
    //    //server certificate validation
    //    System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(RemoteCertificateValidationCallback);

    //    //prepare client side certificate
    //    ImTxnTestWebReference.imTxnServiceWse service = new ImTxnTestWebReference.imTxnServiceWse();
    //    //正式不用测试要用
    //    WebProxy myProxy = new WebProxy(strProxyAddress, true);
    //    myProxy.Credentials = new NetworkCredential(strUserName, strPassword);
    //    service.Proxy = myProxy;
    //    service.Timeout = 1200000;


    //    X509Certificate clientCert = new X509Certificate2(strCertificateAddress, strCertificatePassword);    //the certificate file and the access password
    //    service.ClientCertificates.Add(clientCert);

    //    //set policy and username token
    //    service.SetPolicy("clientPolicy");  //policy name in 'wse3policyCache.config'
    //    UsernameToken token = new UsernameToken(strTokenUserName, strTokenPassword, PasswordOption.SendPlainText);  //username and password of token
    //    service.SetClientCredential(token);
    //    service.Url = strCULTestURL;     //endpoint url of service
    //    return service;
    //} 

    [WebMethod]
    //测试 到CUL
    public string echo(string name)
    {
        //log.Info(LogString("开始","echo",name));
        Log2SQL(0, 1, "echo", "开始"+ " echo"+ name);
        
        ImTxnWebReference.imTxnServiceWse culService = getCULService();
        string strReturn=culService.echo(name);
        //log.Info(LogString("结束", "echo", name));
        Log2SQL(0, 1, "echo", "结束" + " echo" + name);
        
        return strReturn;
    }

    [WebMethod]
    //冻结解冻
    public CodeMsg status(StatusClass CClass,GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                                
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                //prepare service parameter
                ImTxnWebReference.StatBean statBean = new ImTxnWebReference.StatBean(); //parameter object
                statBean.type = CClass.Type;
                statBean.userId = CClass.UserID;
                statBean.merchantNo = CClass.MerchantNo;
                statBean.cardNoFrom = CClass.CardNoFrom;
                statBean.cardNoTo = CClass.CardNoTo;

                //call the service
                Log2SQL(0, 1, "status", "开始" + "冻结解冻" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                ImTxnWebReference.CodeMsg msg = culService.stat(statBean);
                Log2SQL(0, 1, "status", "结束" + "冻结解冻" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);

                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;                
                
                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "status", "开始" + "冻结解冻,没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        }catch(Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "status", "开始" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + GClass.GuID);
            return C4Msg;

        }
    }
    
    [WebMethod]
    //充值，目前由文件充值代替 。
    public CodeMsg islv(IslvClass CClass, GuIDClass GClass)
    {
        try
        {
           if (Iflogin(GClass))
           {
                ImTxnWebReference.IslvBean islvBean = new ImTxnWebReference.IslvBean();
                islvBean.merchantNo = CClass.MerchantNo;
                islvBean.userId = CClass.UserID;
                islvBean.cardNoFrom = CClass.CardNoFrom;
                islvBean.cardNoTo = CClass.CardNoTo;
                islvBean.amount = CClass.Amount;
                islvBean.totalAmount = CClass.TotalAmount;
                islvBean.expiredDate = CClass.ExpiredDate;
                islvBean.discount = CClass.Discount;
                islvBean.buyPerson = CClass.BuyPerson;
                //islvBean.remarks = CClass.Remarks;

                ImTxnWebReference.imTxnServiceWse culService = getCULService();


                //log.Info(LogString("开始", "islv", CClass.MerchantNo + "-" + CClass.CardNoFrom + "-" + CClass.CardNoTo + "-" + CClass.Amount));
                Log2SQL(0, 1, "islv", "开始" + "充值" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);
                ImTxnWebReference.CodeMsg msg = culService.islv(islvBean);
                //log.Info(LogString("结束", "islv", CClass.MerchantNo + "-" + CClass.CardNoFrom + "-" + CClass.CardNoTo + "-" + CClass.Amount));
                Log2SQL(0, 1, "islv", "结束" + "充值" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);

                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "islv", "开始" + "充值" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        }
        catch(Exception e)
        {           
            //log.Info(LogString("结束", "islv", CClass.MerchantNo + "-" + CClass.CardNoFrom + "-" + CClass.CardNoTo + "-" + CClass.Amount));

            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "islv", "开始充值" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    [WebMethod]
    //紧急扣款，测试过目前没用
    public CodeMsg idad(IdadClass CClass, GuIDClass GClass)
    {
        try 
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.IdadBean idadBean = new ImTxnWebReference.IdadBean();
                idadBean.merchantNo = CClass.MerchantNo;
                idadBean.userId = CClass.UserID;
                idadBean.cardNo = CClass.CardNo;
                //idadBean.RCorporateNo = CClass.RCorporateNo;
                idadBean.RMerchantNo = CClass.RMerchantNo;
                idadBean.amount = CClass.Amount;

                Log2SQL(0, 1, "idad","开始" + "紧急扣款" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "-----" + CClass.Amount);
                ImTxnWebReference.CodeMsg msg = culService.idad(idadBean);
                Log2SQL(0, 1, "idad", "结束  " + "紧急扣款" + CClass.MerchantNo + "----" + GClass.GuID+"----" + CClass.CardNo + "-----" + CClass.Amount);

                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "idad", "开始" + "紧急扣款，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "-----" + CClass.Amount);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        }catch(Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "idad", "开始紧急扣款" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNo + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    [WebMethod]
    //转账：目前没用
    public CodeMsg itrf(ItrfClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.ItrfBean itrfBean = new ImTxnWebReference.ItrfBean();
                itrfBean.merchantNo = CClass.MerchantNo;
                itrfBean.userId = CClass.UserID;
                itrfBean.cardNoFrom = CClass.CardNoFrom;
                itrfBean.cardNoTo = CClass.CardNoTo;
                itrfBean.password = CClass.Password;
                itrfBean.amount = CClass.Amount;
                //itrfBean.remarks = CClass.Remarks;
                Log2SQL(0, 1, "itrf", "开始" + "转账" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);
                ImTxnWebReference.CodeMsg msg = culService.itrf(itrfBean);
                Log2SQL(0, 1, "itrf",  "结束  " + "转账" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);

                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "itrf", "开始" + "转账，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }catch(Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "itrf", "开始转账" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNoFrom + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    [WebMethod]
    //换卡
    public CodeMsg ictv(IctvClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.IctvBean ictvBean = new ImTxnWebReference.IctvBean();
                ictvBean.merchantNo = CClass.MerchantNo;
                ictvBean.userId = CClass.UserID;
                ictvBean.cardNoFrom = CClass.CardNoFrom;
                ictvBean.cardNoTo = CClass.CardNoTo;
                ictvBean.password = CClass.Password;
                Log2SQL(0, 1, "ictv",  "开始" + "换卡" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                ImTxnWebReference.CodeMsg msg = culService.ictv(ictvBean);
                Log2SQL(0, 1, "ictv", "结束  " + "换卡" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "ictv", "开始" + "换卡，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch(Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "ictv", "开始换卡" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNoFrom + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    //卡片回收
    [WebMethod]
    public CodeMsg irec(IrecClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.IrecBean irecBean = new ImTxnWebReference.IrecBean();
                irecBean.merchantNo = CClass.MerchantNo;
                irecBean.userId = CClass.UserID;
                irecBean.cardNoFrom = CClass.CardNoFrom;
                irecBean.cardNoTo = CClass.CardNoTo;
                Log2SQL(0, 1, "irec",  "开始" + "卡片回收" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                ImTxnWebReference.CodeMsg msg = culService.irec(irecBean);
                Log2SQL(0, 1, "irec", "结束  " + "卡片回收" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "irec", "开始" + "卡片回收，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch(Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "irec", "开始卡片回收" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNoFrom + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    //充值撤销
    [WebMethod]
    public CodeMsg idvv(IdvvClass CClass, GuIDClass GClass)
    { 
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.IdvvBean idvvBean = new ImTxnWebReference.IdvvBean();
                idvvBean.merchantNo = CClass.MerchantNo;
                idvvBean.userId = CClass.UserID;
                idvvBean.cardNoFrom = CClass.CardNoFrom;
                idvvBean.cardNoTo = CClass.CardNoTo;
                idvvBean.amount = CClass.Amount;
                idvvBean.totalAmount = CClass.TotalAmount;
                idvvBean.DMerchantNo = CClass.DMerchantNo;
                Log2SQL(0, 1, "idvv",  "开始" + "充值撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);
                ImTxnWebReference.CodeMsg msg = culService.idvv(idvvBean);
                Log2SQL(0, 1, "idvv",  "结束  " + "充值撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);
                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "idvv", "开始" + "充值撤销，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNoFrom + "----" + CClass.CardNoTo + "----" + CClass.Amount);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        
        }
        catch(Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "idvv", "开始充值撤销" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNoFrom + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    //密码重置
    [WebMethod]
    public CodeMsg rstp(RstpClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.RstpBean rstp = new ImTxnWebReference.RstpBean();
                rstp.merchantNo = CClass.MerchantNo;
                rstp.userId = CClass.UserId;
                rstp.cardNo = CClass.CardNo;
                rstp.password = CClass.Password;
                Log2SQL(0, 1, "rstp", "开始" + "密码重置" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                ImTxnWebReference.CodeMsg msg = culService.rstp(rstp);
                Log2SQL(0, 1, "rstp", "结束  " + "密码重置" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;
                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "rstp", "开始" + "密码重置,没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        }
        catch(Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "idvv", "开始密码重置" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNo + "----" + GClass.GuID);
            return C4Msg;
        }
    
    }
    //密码修改
    [WebMethod]
    public CodeMsg updp(UpdpClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.UpdpBean updp = new ImTxnWebReference.UpdpBean();
                updp.merchantNo = CClass.MerchantNo;
                updp.userId = CClass.UserId;
                updp.cardNo = CClass.CardNo;
                updp.newPassword = CClass.NewPassword;
                updp.oldPassword = CClass.OldPassword;

                Log2SQL(0, 1, "updp", "开始" + "密码修改" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                ImTxnWebReference.CodeMsg msg = culService.updp(updp);
                Log2SQL(0, 1, "updp", "结束  " + "密码修改" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);

                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;
                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "updp", "开始" + "密码修改,没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "updp", "开始密码修改" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNo + "----" + GClass.GuID);
            return C4Msg;
        }

    }

    
    [WebMethod]
    //券冻结解冻
    public VCodeMsg vstat(VStatusClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {

                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                //prepare service parameter
                ImTxnWebReference.VStatusBean statBean = new ImTxnWebReference.VStatusBean(); //parameter object
                statBean.opeType = CClass.Type;
                statBean.userId = CClass.UserID;
                statBean.merchantNo = CClass.MerchantNo;
                statBean.typeId = CClass.VTypeId;
                statBean.seqNoFrom = CClass.VSeqNoFrom;
                statBean.seqNoTo = CClass.VSeqNoTo;
                statBean.voucherNo = CClass.VoucherNo;
                statBean.reqId = CClass.ReqId;

                //call the service
                Log2SQL(0, 1, "vstatus", "开始" + "券冻结解冻" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VSeqNoFrom + "----" + CClass.VSeqNoTo);
                ImTxnWebReference.VCodeMsg msg = culService.vstat(statBean);
                Log2SQL(0, 1, "vstatus", "结束" + "券冻结解冻" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VSeqNoFrom + "----" + CClass.VSeqNoTo);

                VCodeMsg C4Msg = new VCodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;
                C4Msg.ResId = msg.resId;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "vstatus", "开始" + "券冻结解冻,没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VSeqNoFrom + "----" + CClass.VSeqNoTo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        }
        catch (Exception e)
        {
            VCodeMsg C4Msg = new VCodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "vstatus", "开始券冻结解冻" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.VSeqNoFrom + "----" + CClass.VSeqNoTo + "----" + GClass.GuID);
            return C4Msg;

        }
    }
    //券充值撤销
    [WebMethod]
    public VCodeMsg vidvv(VIdvvClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.VIdvvBean idvvBean = new ImTxnWebReference.VIdvvBean();
                idvvBean.merchantNo = CClass.MerchantNo;
                idvvBean.userId = CClass.UserID;
                idvvBean.typeId = CClass.VTypeId;
                idvvBean.seqNoFrom = CClass.VSeqNoFrom;
                idvvBean.seqNoTo = CClass.VSeqNoTo;
                idvvBean.voucherNo = CClass.VoucherNo;
                idvvBean.number = CClass.VNumber;
                idvvBean.amount = CClass.VAmount;
                idvvBean.totalAmount = CClass.VTotalAmount;
                idvvBean.reqId = CClass.ReqId;


                Log2SQL(0, 1, "vidvv", "开始" + "券充值撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VSeqNoFrom + "----" + CClass.VSeqNoTo + "----" + CClass.VTotalAmount);
                ImTxnWebReference.VCodeMsg msg = culService.vidvv(idvvBean);
                Log2SQL(0, 1, "idvv", "结束  " + "充值撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VSeqNoFrom + "----" + CClass.VSeqNoTo + "----" + CClass.VTotalAmount);
                VCodeMsg C4Msg = new VCodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "vidvv", "开始" + "券充值撤销，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VSeqNoFrom + "----" + CClass.VSeqNoTo + "----" + CClass.VTotalAmount);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            VCodeMsg C4Msg = new VCodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "vidvv", "开始券充值撤销" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.VSeqNoFrom + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    //券密码重置
    [WebMethod]
    public VCodeMsg vrstp(VRstpClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();
  
                ImTxnWebReference.VrstpBean vrstpBean  = new ImTxnWebReference.VrstpBean();
                vrstpBean.merchantNo = CClass.MerchantNo;
                vrstpBean.userId = CClass.UserID;
                vrstpBean.typeId = CClass.VTypeId;
                vrstpBean.voucherNo = CClass.VoucherNo;
                vrstpBean.password = CClass.Password;                
                vrstpBean.reqId = CClass.ReqId;
                vrstpBean.seqNo = CClass.SeqNo;



                Log2SQL(0, 1, "vrstp", "开始" + "券重置密码" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VoucherNo );
                ImTxnWebReference.VCodeMsg msg = culService.vrstp(vrstpBean);
                Log2SQL(0, 1, "vrstp", "结束  " + "券重置密码" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VoucherNo );
                VCodeMsg C4Msg = new VCodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "vrstp", "开始" + "券重置密码，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VoucherNo );
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            VCodeMsg C4Msg = new VCodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "vrstp", "开始券重置密码" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.VoucherNo + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    //券消费撤销
    [WebMethod]
    public VCodeMsg vtxnvoid(VTxnvoidClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.VtxnvoidBean vtxnvoidBean = new ImTxnWebReference.VtxnvoidBean();
                vtxnvoidBean.merchantNo = CClass.MerchantNo;
                vtxnvoidBean.userId = CClass.UserID;                
                vtxnvoidBean.voucherNo = CClass.VoucherNo;
                vtxnvoidBean.termNo = CClass.TermNo;                
                vtxnvoidBean.amount = CClass.VAmount;                
                vtxnvoidBean.reqId = CClass.ReqId;
                vtxnvoidBean.txnSeq = CClass.TxnSeq;

                Log2SQL(0, 1, "vtxnvoid", "开始" + "券消费撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VoucherNo + "----" + CClass.TermNo + "----" + CClass.VAmount);
                ImTxnWebReference.VCodeMsg msg = culService.vtxnvoid(vtxnvoidBean);
                Log2SQL(0, 1, "vtxnvoid", "结束  " + "券消费撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VoucherNo + "----" + CClass.TermNo + "----" + CClass.VAmount);
                VCodeMsg C4Msg = new VCodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "vtxnvoid", "开始" + "券消费撤销，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.VoucherNo + "----" + CClass.VAmount);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            VCodeMsg C4Msg = new VCodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "vtxnvoid", "开始券消费撤销" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.VoucherNo + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    //卡信息查询
    [WebMethod]
    public InfoDataClass info(InfoClass CClass)
    {

        try
        {

            ImTxnWebReference.InfoBean infoBean = new ImTxnWebReference.InfoBean();
            infoBean.merchantNo = CClass.MerchantNo;
            infoBean.userId = CClass.UserID;
            infoBean.cardNoFrom = CClass.CardNoFrom;
            infoBean.cardNoTo = CClass.CardNoTo;
            infoBean.isPager = CClass.IsPager;
            infoBean.pageNo = CClass.PageNo;
            infoBean.pageSize = CClass.PageSize;

            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.InfoData msg = culService.info(infoBean);
            InfoDataClass idc;
            if (msg.codeMsg.code != "OZ")
            {
                idc = new InfoDataClass();
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
                return idc;
            }
            else
            {
                idc = new InfoDataClass(msg.cards.Length);
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
            }


            Int16 i16Count = 0;
            foreach (ImTxnWebReference.Card imCard in msg.cards)
            {
                Card c4Card = new Card();
                c4Card.CardNo = imCard.cardNo;
                c4Card.Balance = imCard.balance;
                c4Card.ActivedDate = imCard.activedDate;
                c4Card.Status = imCard.status;
                c4Card.HotReason = imCard.hotReason;
                c4Card.ExpiredDate = imCard.expiredDate;
                c4Card.IssuerMerchant = imCard.merchantNoName;
                c4Card.IssuerCreateUser = imCard.issuerCreateUser;
                //idc.SetValue(c4Card, i16Count);
                idc.Cards.SetValue(c4Card, i16Count);
                i16Count++;
            }
            return idc;
        }
        catch (Exception e)
        {

            InfoDataClass idc = new InfoDataClass();

            idc.CodeMsg.Code = "NU";
            idc.CodeMsg.Msg = e.Message.Substring(0, 40);

            return idc;

        }
    }
    //卡信息查询测试
    [WebMethod]
    public InfoDataClass infoTest(String sMerchantNo, String sCardNo)
    {

        try
        {

            ImTxnWebReference.InfoBean infoBean = new ImTxnWebReference.InfoBean();
            infoBean.merchantNo = sMerchantNo;
            infoBean.userId = sMerchantNo;
            infoBean.cardNoFrom = sCardNo;
            infoBean.cardNoTo = sCardNo;
            infoBean.isPager = "N";
            infoBean.pageNo = "1";

            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.InfoData msg = culService.info(infoBean);
            InfoDataClass idc;
            if (msg.codeMsg.code != "OZ")
            {
                idc = new InfoDataClass();
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
                return idc;
            }
            else
            {
                idc = new InfoDataClass(msg.cards.Length);
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
            }


            Int16 i16Count = 0;
            foreach (ImTxnWebReference.Card imCard in msg.cards)
            {
                Card c4Card = new Card();
                c4Card.CardNo = imCard.cardNo;
                c4Card.Balance = imCard.balance;
                c4Card.ActivedDate = imCard.activedDate;
                c4Card.Status = imCard.status;
                c4Card.HotReason = imCard.hotReason;
                c4Card.ExpiredDate = imCard.expiredDate;
                c4Card.IssuerMerchant = imCard.merchantNoName;
                c4Card.IssuerCreateUser = imCard.issuerCreateUser;
                //idc.SetValue(c4Card, i16Count);
                idc.Cards.SetValue(c4Card, i16Count);
                i16Count++;
            }
            return idc;
        }
        catch (Exception e)
        {

            InfoDataClass idc = new InfoDataClass();

            idc.CodeMsg.Code = "NU";
            idc.CodeMsg.Msg = e.Message.Substring(0, 40);

            return idc;

        }
    }
    //券信息查询
    [WebMethod]
    public VInfoDataClass vinfo(VInfoClass CClass)
    {

        try
        {

            ImTxnWebReference.VInfoBean vinfoBean = new ImTxnWebReference.VInfoBean();
            vinfoBean.merchantNo = CClass.MerchantNo;
            vinfoBean.userId = CClass.UserID;
            vinfoBean.typeId = CClass.TypeId;
            vinfoBean.seqNoFrom = CClass.SeqNoFrom;
            vinfoBean.seqNoTo = CClass.SeqNoTo;
            vinfoBean.voucherNo = CClass.VoucherNo;
            vinfoBean.isPager = CClass.IsPager;
            vinfoBean.pageNo = CClass.PageNo;
            vinfoBean.pageSize = CClass.PageSize;    
            

            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.VInfoData vmsg = culService.vinfo(vinfoBean);
            VInfoDataClass idc;
            if (vmsg.codeMsg.code != "01")
            {
                idc = new VInfoDataClass();
                idc.Total = vmsg.total;

                idc.VCodeMsg.Code = vmsg.codeMsg.code;
                idc.VCodeMsg.Msg = vmsg.codeMsg.msg;
                return idc;
            }
            else
            {
                idc = new VInfoDataClass(vmsg.vouchers.Length);
                idc.Total = vmsg.total;

                idc.VCodeMsg.Code = vmsg.codeMsg.code;
                idc.VCodeMsg.Msg = vmsg.codeMsg.msg;
            }


            Int16 i16Count = 0;
            foreach (ImTxnWebReference.Voucher imVoucher in vmsg.vouchers)
            {
                Voucher c4Voucher = new Voucher();
                c4Voucher.VoucherNo = imVoucher.voucherNo;
                c4Voucher.SeqNo = imVoucher.seqNo;
                c4Voucher.TypeId = imVoucher.typeId;
                c4Voucher.Status = imVoucher.status;
                c4Voucher.ExpiredDate = imVoucher.expiredDate;
                c4Voucher.ActivedDMer = imVoucher.activeMer;
                c4Voucher.ActivedDate = imVoucher.activeDate;
                c4Voucher.UseMer = imVoucher.useMer;
                c4Voucher.UseTime = imVoucher.useTime;
                c4Voucher.Amount = imVoucher.amount;
                c4Voucher.ActivedMerName = imVoucher.activeMerName;
                c4Voucher.UseMerName = imVoucher.useMerName;
                
                //idc.SetValue(c4Card, i16Count);
                idc.Vouchers.SetValue(c4Voucher, i16Count);
                i16Count++;
            }
            return idc;
        }
        catch (Exception e)
        {

            VInfoDataClass idc = new VInfoDataClass();

            idc.VCodeMsg.Code = "NU";
            idc.VCodeMsg.Msg = e.Message.Substring(0, 40);

            return idc;

        }
    }
    //卡销售查询
    [WebMethod]
    public CtqyDataClass ctqy(CtqyClass CClass)
    {
        try
        {
            ImTxnWebReference.imTxnServiceWse culService  = getCULService();

            ImTxnWebReference.CtqyBean ctqyBean = new ImTxnWebReference.CtqyBean();
            ctqyBean.userId = CClass.UserID;
            ctqyBean.merchantNo = CClass.MerchantNo;
            ctqyBean.cardNo = CClass.CardNo;
            ctqyBean.queryType = CClass.QueryType;
            ctqyBean.dateFrom = CClass.DateFrom;
            ctqyBean.dateTo = CClass.DateTo;
            ctqyBean.isPager = CClass.IsPager;
            ctqyBean.pageNo = CClass.PageNo;
            ctqyBean.pageSize = CClass.PageSize;

            ImTxnWebReference.CtqyData msg = culService.ctqy(ctqyBean);
            CtqyDataClass cdc;

            if (msg.codeMsg.code != "PZ" || (msg.codeMsg.code == "PZ" && msg.total == "0"))
            {
                cdc = new CtqyDataClass();
                cdc.Total = msg.total;
                cdc.CodeMsg.Code = msg.codeMsg.code;
                cdc.CodeMsg.Msg = msg.codeMsg.msg;
                return cdc;
            }
            else
            {
                cdc = new CtqyDataClass(msg.cardTxns.Length);
                cdc.Total = msg.total;
                cdc.CodeMsg.Code = msg.codeMsg.code;
                cdc.CodeMsg.Msg = msg.codeMsg.msg;
            }
            
            Int16 i16Count = 0;
            foreach (ImTxnWebReference.CardTxn imCardTxn in msg.cardTxns)
            {
                
                CardTxn cardTxn = new CardTxn();
                cardTxn.TxnId = imCardTxn.txnId;
                cardTxn.InputChannel = imCardTxn.inputChannel;
                cardTxn.TxnCode = imCardTxn.txnCode;
                cardTxn.EarnAmount = imCardTxn.earnAmount;
                cardTxn.RedeemAmount = imCardTxn.redeemAmount;
                cardTxn.TransferAmount = imCardTxn.transferAmount;
                cardTxn.AdjustAmount = imCardTxn.adjustAmount;
                cardTxn.Status = imCardTxn.status;
                cardTxn.TxnDate = imCardTxn.txnDate;
                cardTxn.TxnTime = imCardTxn.txnTime;
                cardTxn.MerchantNo = imCardTxn.merchantNo;
                cardTxn.TermNo = imCardTxn.termNo;
                
                cdc.CardTxns.SetValue(cardTxn,i16Count);
                i16Count++;
            }
            return cdc;
        }
        catch(Exception e)
        {
            CtqyDataClass cdc = new CtqyDataClass();
            cdc.CodeMsg.Code = "NU";
            
            cdc.CodeMsg.Msg = e.Message.Substring(0,2000);   
            return cdc;
                 
        }
    }
    [WebMethod]
    public CtqyDataClass ctqyTest()
    {
        try
        {
            ImTxnWebReference.imTxnServiceWse culService = getCULService();

            ImTxnWebReference.CtqyBean ctqyBean = new ImTxnWebReference.CtqyBean();
            ctqyBean.userId = "102210054111029";
            ctqyBean.merchantNo = "102210054111029";
            ctqyBean.cardNo = "2336840209900006071";
            ctqyBean.queryType = "H";
            ctqyBean.dateFrom = "20170523";
            ctqyBean.dateTo = "20171121";
            ctqyBean.isPager = "N";
            ctqyBean.pageNo = "1";
            ctqyBean.pageSize = "1000000";

            ImTxnWebReference.CtqyData msg = culService.ctqy(ctqyBean);
            CtqyDataClass cdc;

            if (msg.codeMsg.code != "PZ" || (msg.codeMsg.code == "PZ" && msg.total == "0"))
            {
                cdc = new CtqyDataClass();
                cdc.Total = msg.total;
                cdc.CodeMsg.Code = msg.codeMsg.code;
                cdc.CodeMsg.Msg = msg.codeMsg.msg;
                return cdc;
            }
            else
            {
                cdc = new CtqyDataClass(msg.cardTxns.Length);
                cdc.Total = msg.total;
                cdc.CodeMsg.Code = msg.codeMsg.code;
                cdc.CodeMsg.Msg = msg.codeMsg.msg;
            }

            Int16 i16Count = 0;
            foreach (ImTxnWebReference.CardTxn imCardTxn in msg.cardTxns)
            {

                CardTxn cardTxn = new CardTxn();
                cardTxn.TxnId = imCardTxn.txnId;
                cardTxn.InputChannel = imCardTxn.inputChannel;
                cardTxn.TxnCode = imCardTxn.txnCode;
                cardTxn.EarnAmount = imCardTxn.earnAmount;
                cardTxn.RedeemAmount = imCardTxn.redeemAmount;
                cardTxn.TransferAmount = imCardTxn.transferAmount;
                cardTxn.AdjustAmount = imCardTxn.adjustAmount;
                cardTxn.Status = imCardTxn.status;
                cardTxn.TxnDate = imCardTxn.txnDate;
                cardTxn.TxnTime = imCardTxn.txnTime;
                cardTxn.MerchantNo = imCardTxn.merchantNo;
                cardTxn.TermNo = imCardTxn.termNo;
                
                cdc.CardTxns.SetValue(cardTxn, i16Count);
                i16Count++;
            }
            return cdc;
        }
        catch (Exception e)
        {
            CtqyDataClass cdc = new CtqyDataClass();
            cdc.CodeMsg.Code = "NU";

            cdc.CodeMsg.Msg = e.Message.Substring(0, 2000);
            return cdc;

        }
    }
    //券销售查询
    [WebMethod]
    public VtqyDataClass vtqy(VtqyClass CClass)
    {
        try
        {
            ImTxnWebReference.imTxnServiceWse culService = getCULService();

            ImTxnWebReference.VtqyBean vtqyBean = new ImTxnWebReference.VtqyBean();
            vtqyBean.userId = CClass.UserID;
            vtqyBean.merchantNo = CClass.MerchantNo;
            vtqyBean.voucherNo = CClass.VoucherNo;
            vtqyBean.termNo = CClass.TermNo;
            vtqyBean.queryType = CClass.QueryType;
            vtqyBean.dateFrom = CClass.DateFrom;
            vtqyBean.dateTo = CClass.DateTo;
            vtqyBean.isPager = CClass.IsPager;
            vtqyBean.pageNo = CClass.PageNo;
            vtqyBean.pageSize = CClass.PageSize;
            vtqyBean.typeId = CClass.TypeId;
            vtqyBean.seqNo = CClass.SeqNo;


            ImTxnWebReference.VtqyData vmsg = culService.vtqy(vtqyBean);
            VtqyDataClass cdc;

            if ((vmsg.codeMsg.code != "00" || (vmsg.codeMsg.code == "00" && vmsg.total == "0") ) && (vmsg.codeMsg.code != "01" || (vmsg.codeMsg.code == "01" && vmsg.total == "0")))
            {
                cdc = new VtqyDataClass();
                cdc.Total = vmsg.total;
                cdc.VCodeMsg.Code = vmsg.codeMsg.code;
                cdc.VCodeMsg.Msg = vmsg.codeMsg.msg;
                return cdc;
            }
            else
            {
                cdc = new VtqyDataClass(vmsg.voucherTxns.Length);
                cdc.Total = vmsg.total;
                cdc.VCodeMsg.Code = vmsg.codeMsg.code;
                cdc.VCodeMsg.Msg = vmsg.codeMsg.msg;
            }

            Int16 i16Count = 0;
            foreach (ImTxnWebReference.VoucherTxn imVoucherTxn in vmsg.voucherTxns )
            {

                VoucherTxn voucherTxn = new VoucherTxn();
                voucherTxn.TxnId = imVoucherTxn.txnId;
                voucherTxn.SettleDate = imVoucherTxn.settleDate;
                voucherTxn.MerNo = imVoucherTxn.merNo;
                voucherTxn.TermNo = imVoucherTxn.termNo;
                voucherTxn.TraceNo = imVoucherTxn.traceNo;
                voucherTxn.BatchNo = imVoucherTxn.batchNo;
                voucherTxn.RefNo = imVoucherTxn.refNo;
                voucherTxn.RespCode = imVoucherTxn.respCode;
                voucherTxn.OperId = imVoucherTxn.operId;
                voucherTxn.VoucherNo = imVoucherTxn.voucherNo;
                voucherTxn.Amount = imVoucherTxn.amount;
                voucherTxn.CardNo = imVoucherTxn.cardNo;
                voucherTxn.ICNo = imVoucherTxn.ICNo;
                voucherTxn.TranCode = imVoucherTxn.tranCode;
                voucherTxn.TranMode = imVoucherTxn.tranMode;
                voucherTxn.MerName = imVoucherTxn.merName;
                voucherTxn.OperType = imVoucherTxn.operType;
                voucherTxn.CreateTimestamp = imVoucherTxn.createTimestamp;
                voucherTxn.CreateUser = imVoucherTxn.createUser;

                cdc.VoucherTxns.SetValue(voucherTxn, i16Count);
                i16Count++;
            }
            return cdc;
        }
        catch (Exception e)
        {
            VtqyDataClass cdc = new VtqyDataClass();
            cdc.VCodeMsg.Code = "NU";

            cdc.VCodeMsg.Msg = e.Message.Substring(0, 2000);
            return cdc;

        }
    }
    //查询余额
    [WebMethod]
    public string getBalance(InfoClass CClass)
    {
        try
        {           

            ImTxnWebReference.InfoBean infoBean = new ImTxnWebReference.InfoBean();
            infoBean.merchantNo = CClass.MerchantNo;
            infoBean.userId = CClass.UserID;
            infoBean.cardNoFrom = CClass.CardNoFrom;
            infoBean.cardNoTo = CClass.CardNoTo;
            infoBean.isPager = CClass.IsPager;
            infoBean.pageNo = CClass.PageNo;
            infoBean.pageSize = CClass.PageSize;

            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.InfoData msg = culService.info(infoBean);

            string strResult;
            if (msg.codeMsg.code != "OZ")
            {
                strResult = msg.codeMsg.code + " " + msg.codeMsg.msg; ;
            }
            else
            {
                strResult= msg.cards[0].balance;
            }
            return strResult;
        }
        catch (Exception e)
        {
            return e.Message;
            //"The underlying connection was closed: An unexpected error occurred on a send."
        }    
    
    }
    //IC卡信息查询
    [WebMethod]
    public IcDataClass icInfo(IcClass CClass)
    {

        try
        {
    
            ImTxnWebReference.IcInfoByAllBean infoBean = new ImTxnWebReference.IcInfoByAllBean();
            infoBean.merchantNo = CClass.MerchantNo;
            infoBean.userId = CClass.UserID;
            infoBean.icType = CClass.IcType;
            infoBean.cardNoFrom = CClass.CardNoFrom;
            infoBean.cardNoTo = CClass.CardNoTo;
            infoBean.isPager = CClass.IsPager;
            infoBean.pageNo = CClass.PageNo;
            infoBean.pageSize = CClass.PageSize;

            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.IcInfoByAllData msg = culService.icInfoByAll(infoBean);
            IcDataClass idc;
            if (msg.codeMsg.code != "00")
            {
                idc = new IcDataClass();
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
                return idc;
            }
            else
            {
                idc = new IcDataClass(msg.cards.Length);
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
            }
   
            Int16 i16Count = 0;
            foreach (ImTxnWebReference.IcCard  imCard in msg.cards)
            {
                IcCard c4Card = new IcCard();
                c4Card.CardNo = imCard.cardNo;
                c4Card.Balance = imCard.balance;
                c4Card.ActivedDate = imCard.activedDate;
                c4Card.Status = imCard.status;
                c4Card.HotReason = imCard.hotReason;
                c4Card.ExpiredDate = imCard.expiredDate;
                c4Card.MerchantNoName = imCard.merchantNoName;
                c4Card.StatusName = imCard.statusName;
                c4Card.HotReasonName = imCard.hotReasonName;
                
                //idc.SetValue(c4Card, i16Count);
                idc.Cards.SetValue(c4Card, i16Count);
                i16Count++;
            }
            return idc;
        }
        catch (Exception e)
        {

            IcDataClass idc = new IcDataClass();

            idc.CodeMsg.Code = "NU";
            idc.CodeMsg.Msg = e.Message.Substring(0, 40);

            return idc;

        }
    }
    [WebMethod]
    //IC卡销卡
    public IcDestoryDataClass icDestoryCard(IcDestoryClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {

                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                //prepare service parameter
                ImTxnWebReference.IcDestoryCardBean statBean = new ImTxnWebReference.IcDestoryCardBean(); //parameter object
               
                statBean.merchantNo = CClass.MerchantNo;
                statBean.userId = CClass.UserID;
                statBean.icType = CClass.IcType;         
                statBean.cardNo = CClass.CardNo;
                statBean.hotReason = CClass.HotReason;
                statBean.reqId = CClass.ReqId;

                //call the service
                Log2SQL(0, 1, "icDestoryCard", "开始" + "IC销卡" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                ImTxnWebReference.IcDestoryCardData msg = culService.icDestoryCard(statBean);
                Log2SQL(0, 1, "icDestoryCard", "结束" + "IC销卡" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);

                IcDestoryDataClass icDDC = new IcDestoryDataClass();
                icDDC.CodeMsg.Code = msg.codeMsg.code;
                icDDC.CodeMsg.Msg = msg.codeMsg.msg;
                icDDC.Balance = msg.balance;
                icDDC.ReqId = msg.resId;

                return icDDC;
            }
            else
            {
                Log2SQL(0, 1, "icDestoryCard", "开始" + "IC销卡,没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        }
        catch (Exception e)
        {
            IcDestoryDataClass icDDC = new IcDestoryDataClass();
            icDDC.CodeMsg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                icDDC.CodeMsg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                icDDC.CodeMsg.Msg = e.Message;
            }
            Log2SQL(0, 1, "icDestoryCard", "开始" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNo + "----" + CClass.CardNo + "----" + GClass.GuID);
            return icDDC;

        }
    }
    //激活撤销
    [WebMethod]
    public InactiveDataClass inactive(InactiveClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.InactiveBean inactiveBean = new ImTxnWebReference.InactiveBean();
                inactiveBean.merchantNo = CClass.MerchantNo;
                inactiveBean.userId = CClass.UserID;
                inactiveBean.cardNo = CClass.CardNo;
                inactiveBean.ean = CClass.Ean;
                inactiveBean.refNo = CClass.RefNo;
                inactiveBean.txnTime = CClass.TxnTime;
                inactiveBean.reqTxnDate = CClass.ReqTxnDate;
                inactiveBean.reqTxnTime = CClass.ReqTxnTime;
                inactiveBean.reqId = CClass.ReqId;
                inactiveBean.opeType = CClass.OpeType;
                
                

                Log2SQL(0, 1, "inactive", "开始" + "激活撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
                ImTxnWebReference.InactiveData msg = culService.inactive(inactiveBean);
                Log2SQL(0, 1, "inactive", "结束  " + "激活撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);

                InactiveDataClass idc = new InactiveDataClass();
    
                idc.MerchantNo = msg.merchantNo;
                idc.UserID = msg.userId;
                idc.CardNo = msg.cardNo;
                idc.Ean = msg.ean;
                idc.CardPrice = msg.cardPrice;
                idc.SaleAmount = msg.saleAmount;
                idc.RefNo = msg.refNo;
                idc.TxnTime = msg.txnTime;
                idc.ReqTxnDate = msg.resTxnDate;
                idc.ReqTxnTime = msg.resTxnTime;
                idc.ReqId = msg.resId;
                idc.Code = msg.code;
                idc.Msg = msg.msg;


                return idc;
            }
            else
            {
                Log2SQL(0, 1, "inactive", "开始" + "激活撤销，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            InactiveDataClass idc = new InactiveDataClass();
            
            if (e.Message.Length > 2000)
            {
                idc.Code = "NU";
                idc.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                idc.Code = "NU";
                idc.Msg = e.Message;
            }
            Log2SQL(0, 1, "inactive",  "激活撤销，出现异常！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
            return idc;
        }
    }

    //离线手工激活
    [WebMethod]
    public OfflineActiveDataClass offlineActiveC4p(OfflineActiveClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.OfflineActiveBean offlineActiveBean = new ImTxnWebReference.OfflineActiveBean();

                offlineActiveBean.merchantNo = CClass.MerchantNo;
                offlineActiveBean.userId = CClass.UserID;
                offlineActiveBean.cardNo = CClass.CardNo;
                offlineActiveBean.ean = CClass.Ean;

                Log2SQL(0, 1, "offlineActiveC4p", "开始" + "激活撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
                ImTxnWebReference.OfflineActiveData msg = culService.offlineActiveC4p(offlineActiveBean);
                Log2SQL(0, 1, "offlineActiveC4p", "结束  " + "激活撤销" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);

                OfflineActiveDataClass idc = new OfflineActiveDataClass();
 
                idc.MerchantNo = msg.merchantNo;
                idc.TermNo = msg.termNo;
                idc.Code = msg.code;
                idc.Msg = msg.msg;
                idc.ProductName = msg.productName;
                idc.CardNo = msg.cardNo;
                idc.Ean = msg.ean;

                idc.TxnDate = msg.txnDate;
                idc.TxnTime = msg.txnTime;
                idc.TxnType = msg.txnType;

                idc.PosBatchNo = msg.posBatchNo;
                idc.PosRefNo = msg.posRefNo;
                idc.RetriRefNo = msg.retriRefNo;
                idc.CardPrice = msg.cardPrice;
                idc.SalePrice = msg.salePrice;
                idc.ManageCard = msg.manageCard;
              


                return idc;
            }
            else
            {
                Log2SQL(0, 1, "offlineActiveC4p", "开始" + "离线手工激活，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            OfflineActiveDataClass idc = new OfflineActiveDataClass();

            if (e.Message.Length > 2000)
            {
                idc.Code = "NU";
                idc.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                idc.Code = "NU";
                idc.Msg = e.Message;
            }
            Log2SQL(0, 1, "offlineActiveC4p", "激活撤销，出现异常！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
            return idc;
        }
    }
    //对一张分销卡片进行激活操作
    [WebMethod]
    public ActiveDataClass active(ActiveClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.ActiveBean activeBean = new ImTxnWebReference.ActiveBean();
    
                activeBean.merchantNo = CClass.MerchantNo;
                activeBean.userId = CClass.UserID;
                activeBean.cardNo = CClass.CardNo;
                activeBean.ean = CClass.Ean;
                activeBean.salePrice = CClass.SalePrice;
                activeBean.reqTxnDate = CClass.ReqTxnDate;
                activeBean.reqTxnTime = CClass.ReqTxnTime;
                activeBean.reqId = CClass.ReqId;

                Log2SQL(0, 1, "active", "开始" + "单卡充值" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
                ImTxnWebReference.ActiveData msg = culService.active(activeBean);
                Log2SQL(0, 1, "active", "结束  " + "单卡充值" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);

                ActiveDataClass idc = new ActiveDataClass();
    
                idc.Code = msg.code;
                idc.Msg = msg.msg;
                idc.MerchantNo = msg.merchantNo;
                idc.UserID = msg.userId;                
                idc.CardNo = msg.cardNo;
                idc.Ean = msg.ean;
                idc.SalePrice = msg.salePrice;
                idc.ReqTxnDate = msg.resTxnDate;
                idc.ReqTxnTime = msg.resTxnTime;
                idc.ReqId = msg.resId;
      
                return idc;
            }
            else
            {
                Log2SQL(0, 1, "active", "开始" + "单卡充值，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            ActiveDataClass idc = new ActiveDataClass();

            if (e.Message.Length > 2000)
            {
                idc.Code = "NU";
                idc.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                idc.Code = "NU";
                idc.Msg = e.Message;
            }
            Log2SQL(0, 1, "active", "单卡充值，出现异常！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo + "----" + CClass.Ean);
            return idc;
        }
    }

    //卡片延期接口
    [WebMethod]
    public InfoDataClass batchPostpone(BatchPostponeClass CClass)
    {
        try
        {
            ImTxnWebReference.BatchPostponeBean infoBean = new ImTxnWebReference.BatchPostponeBean();
            infoBean.merchantNo = CClass.MerchantNo;
            infoBean.userId = CClass.UserID;
            infoBean.cardNoFrom = CClass.CardNoFrom;
            infoBean.cardNoTo = CClass.CardNoTo;
            infoBean.reqExpireDate = CClass.ReqExpireDate;

            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.CodeMsg msg = culService.batchPostpone(infoBean);

            InfoDataClass idc = new InfoDataClass();
            idc.CodeMsg.Code = msg.code;
            idc.CodeMsg.Msg = msg.msg;
            return idc;
        }
        catch (Exception e)
        {
            InfoDataClass idc = new InfoDataClass();
            idc.CodeMsg.Code = "NU";
            idc.CodeMsg.Msg = e.Message.Substring(0, 40);
            return idc;
        }
    }

    public static bool RemoteCertificateValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        //Return True to force the certificate to be accepted.
        return true;
    }
    ///<summary> 
    //依据连接串名字connectionName返回数据连接字符串  
    ///</summary> 
    ///<param name="connectionName"></param> 
    ///<returns></returns> 
    private static string GetConnectionStringsConfig(string connectionName)
    {
        string connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString.ToString();
        return connectionString;
    }
    ///<summary> 
    ///返回＊.exe.config文件中appSettings配置节的value项  
    ///</summary> 
    ///<param name="strKey"></param> 
    ///<returns></returns> 
    private static string GetAppConfig(string strKey)
    {
        foreach (string key in ConfigurationManager.AppSettings)
        {
            if (key == strKey)
            {
                return ConfigurationManager.AppSettings[strKey];
            }
        }
        return null;
    }

    //加密：
    private String Encrypt(String strText, String strEncrKey)
    {
        Byte[] byKey =   { };
        Byte[] IV =   { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
        try
        {
            byKey = System.Text.Encoding.UTF8.GetBytes(strEncrKey.Substring(0, 8));
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            Byte[] inputByteArray = Encoding.UTF8.GetBytes(strText);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(byKey, IV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    } 
    //解密：
    private String Decrypt(String strText, String sDecrKey)
    {
        Byte[] byKey =   { };
        Byte[] IV =   { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
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
    //取客户端的IP地址
    //private string GetCustomerIP()
    //{
    //    string CustomerIP = "";
    //    if (HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
    //    {
    //        CustomerIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();

    //    }
    //    else
    //    {
    //        CustomerIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
    //    }

    //    //string strHTTP_X_FORWARDED_FOR = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
    //    //string strREMOTE_ADDR = HttpContext.Current.Request.ServerVariables["REMOTE_HOST"].ToString();
    //    ////if (strHTTP_X_FORWARDED_FOR != "10.132.200.5" && strHTTP_X_FORWARDED_FOR != "")
    //    ////{
    //    ////    CustomerIP = strHTTP_X_FORWARDED_FOR;
    //    ////}
    //    ////else
    //    ////{
    //    ////    CustomerIP = strREMOTE_ADDR;
    //    ////}

    //    //Log2SQL(0, 1, "test", "strHTTP_X_FORWARDED_FOR=" + strHTTP_X_FORWARDED_FOR + ";strREMOTE_ADDR=" + strREMOTE_ADDR);



    //    return CustomerIP;
    //}
    //判断是否在登录列表里
    //private bool Iflogin(string strIPAddress)
    //{
    //    bool bReturn = false;
    //    try
    //    {
    //        if (strIPAddress.Equals("10.132.200.5") || strIPAddress.Equals("127.0.0.1"))
    //        {
    //            bReturn = true;
    //        }
    //        else{
    //            using (SqlConnection sqlConn = new SqlConnection(strSqlConn2))
    //            {
    //                using (SqlCommand sqlComm = new SqlCommand())
    //                {
    //                    sqlConn.Open();
    //                    sqlComm.Connection = sqlConn;
    //                    sqlComm.CommandText = String.Format("SELECT *  FROM [ShoppingCard].[dbo].[LoginInfo] where IsLogin=1 and ComputerIP='{0}'", strIPAddress);
    //                    SqlDataAdapter sqlDA = new SqlDataAdapter(sqlComm);
    //                    DataSet ds = new DataSet();
    //                    sqlDA.Fill(ds);

    //                    if (ds.Tables[0].Rows.Count > 0)
    //                    {
    //                        bReturn = true;
    //                    }
    //                }
    //            }
    //        }

    //        return bReturn;
    //    }
    //    catch
    //    {
    //        return bReturn;
    //    }
    //}

    //判断GuID是否正确
    private bool Iflogin(GuIDClass GClass)
    {
        bool bReturn = false;
        try
        {
            if (GClass.GuID.Equals(""))
            {}
            else
            {
                using (SqlConnection sqlConn = new SqlConnection(strSqlConn2))
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


    //设置日志内容
    private string LogString(string startOrEnd,string methodName,string parameterContent)
    {
        string strReturn = "";
        strReturn = String.Format("{6} webservice <method>:{0};<parameter>:{1};<IP>:{2};<Forword IP>:{3};<Host Name>:{4};<User Name>:{5}", methodName, parameterContent, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"], HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"], HttpContext.Current.Request.ServerVariables["REMOTE_HOST"], HttpContext.Current.Request.ServerVariables["REMOTE_USER"], startOrEnd);
        return strReturn;
    }
    //写数据库日志
    private void Log2SQL(int intLevel, int intModule, string strMothed, string strMessage)
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
        string strModule = "";
        if (intModule == 0)
        {
            strModule = "ChargeByFile";
        }
        else if (intModule == 1)
        {
            strModule = "SpecialOperation";
        }
        else
        {
            strModule = "Other";
        }

        using (SqlConnection sqlConn = new SqlConnection(strSqlConn))
        {
            using (SqlCommand sqlComm = new SqlCommand())
            {
                sqlConn.Open();
                sqlComm.Connection = sqlConn;
                sqlComm.CommandText = String.Format("insert into ActivationLogForSpecialOperation(Date,Level,Module,Mothed,Message) values('{0}','{1}','{2}','{3}','{4}')", DateTime.Now.ToString(), strLevel, strModule, strMothed, strMessage);
                sqlComm.ExecuteNonQuery();
            }
        }
    }

    //'modify code 036:start------------------------------------------------------------------------- 
    //保龙仓卡（中行卡）信息查询
    [WebMethod]
    public InfoDataClass m27j27CardInfo(M27j27CardInfoClass CClass)
    {

        try
        {

            ImTxnWebReference.M27j27CardInfoBean M27j27CardInfoBean = new ImTxnWebReference.M27j27CardInfoBean();
            M27j27CardInfoBean.merchantNo = CClass.MerchantNo;
            M27j27CardInfoBean.userId = CClass.UserID;
            M27j27CardInfoBean.cardNo = CClass.CardNo;
            M27j27CardInfoBean.isPager = CClass.IsPager;
            M27j27CardInfoBean.pageNo = CClass.PageNo;
            M27j27CardInfoBean.pageSize = CClass.PageSize;

            ImTxnWebReference.imTxnServiceWse culService = getCULService();
            ImTxnWebReference.InfoData msg = culService.m27j27CardInfo(M27j27CardInfoBean);
            InfoDataClass idc;
            if (msg.codeMsg.code != "OM")
            {
                idc = new InfoDataClass();
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
                return idc;
            }
            else
            {
                idc = new InfoDataClass(msg.cards.Length);
                idc.Total = msg.total;

                idc.CodeMsg.Code = msg.codeMsg.code;
                idc.CodeMsg.Msg = msg.codeMsg.msg;
            }


            Int16 i16Count = 0;
            foreach (ImTxnWebReference.Card imCard in msg.cards)
            {
                Card c4Card = new Card();
                c4Card.CardNo = imCard.cardNo;
                c4Card.Balance = imCard.balance;
                c4Card.ActivedDate = imCard.activedDate;
                c4Card.Status = imCard.status;
                c4Card.HotReason = imCard.hotReason;
                c4Card.ExpiredDate = imCard.expiredDate;
                c4Card.IssuerMerchant = imCard.merchantNoName;
                c4Card.IssuerCreateUser = imCard.issuerCreateUser;
                //idc.SetValue(c4Card, i16Count);
                idc.Cards.SetValue(c4Card, i16Count);
                i16Count++;
            }
            return idc;
        }
        catch (Exception e)
        {

            InfoDataClass idc = new InfoDataClass();

            idc.CodeMsg.Code = "NU";
            idc.CodeMsg.Msg = e.Message.Substring(0, 40);

            return idc;

        }
    }
    //保龙仓卡（中行卡）交易明细查询
    [WebMethod]
    public CtqyDataClass m27j27CardTxn(M27j27CardTxnClass CClass)
    {
        try
        {
            ImTxnWebReference.imTxnServiceWse culService = getCULService();

            ImTxnWebReference.M27j27CardTxnBean M27j27CardTxnBean = new ImTxnWebReference.M27j27CardTxnBean();
            M27j27CardTxnBean.userId = CClass.UserID;
            M27j27CardTxnBean.merchantNo = CClass.MerchantNo;
            M27j27CardTxnBean.cardNo = CClass.CardNo;
            M27j27CardTxnBean.queryType = CClass.QueryType;
            M27j27CardTxnBean.dateFrom = CClass.DateFrom;
            M27j27CardTxnBean.dateTo = CClass.DateTo;
            M27j27CardTxnBean.isPager = CClass.IsPager;
            M27j27CardTxnBean.pageNo = CClass.PageNo;
            M27j27CardTxnBean.pageSize = CClass.PageSize;

            ImTxnWebReference.CtqyData msg = culService.m27j27CardTxn(M27j27CardTxnBean);
            CtqyDataClass cdc;

            if (msg.codeMsg.code != "OO" || (msg.codeMsg.code == "OO" && msg.total == "0"))
            {
                cdc = new CtqyDataClass();
                cdc.Total = msg.total;
                cdc.CodeMsg.Code = msg.codeMsg.code;
                cdc.CodeMsg.Msg = msg.codeMsg.msg;
                return cdc;
            }
            else
            {
                cdc = new CtqyDataClass(msg.cardTxns.Length);
                cdc.Total = msg.total;
                cdc.CodeMsg.Code = msg.codeMsg.code;
                cdc.CodeMsg.Msg = msg.codeMsg.msg;
            }

            Int16 i16Count = 0;
            foreach (ImTxnWebReference.CardTxn imCardTxn in msg.cardTxns)
            {

                CardTxn cardTxn = new CardTxn();
                cardTxn.TxnId = imCardTxn.txnId;
                cardTxn.InputChannel = imCardTxn.inputChannel;
                cardTxn.TxnCode = imCardTxn.txnCode;
                cardTxn.EarnAmount = imCardTxn.earnAmount;
                cardTxn.RedeemAmount = imCardTxn.redeemAmount;
                cardTxn.TransferAmount = imCardTxn.transferAmount;
                cardTxn.AdjustAmount = imCardTxn.adjustAmount;
                cardTxn.Status = imCardTxn.status;
                cardTxn.TxnDate = imCardTxn.txnDate;
                cardTxn.TxnTime = imCardTxn.txnTime;
                cardTxn.MerchantNo = imCardTxn.merchantNo;
                cardTxn.TermNo = imCardTxn.termNo;

                cdc.CardTxns.SetValue(cardTxn, i16Count);
                i16Count++;
            }
            return cdc;
        }
        catch (Exception e)
        {
            CtqyDataClass cdc = new CtqyDataClass();
            cdc.CodeMsg.Code = "NU";

            cdc.CodeMsg.Msg = e.Message.Substring(0, 2000);
            return cdc;

        }
    }
    //保龙仓卡（中行卡）销卡
    [WebMethod]
    public DestoryDataClass DestoryCard(DestoryClass CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {

                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                //prepare service parameter
                ImTxnWebReference.DestoryBean DestoryBean = new ImTxnWebReference.DestoryBean(); //parameter object

                DestoryBean.merchantNo = CClass.MerchantNo;
                DestoryBean.userId = CClass.UserID;
                DestoryBean.cardNo = CClass.CardNo;
                //DestoryBean.hotReason = CClass.HotReason;

                //call the service
                Log2SQL(0, 1, "DestoryCard", "开始" + "保龙仓卡（中行卡）销卡" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                ImTxnWebReference.DestoryData msg = culService.destoryCard(DestoryBean);
                Log2SQL(0, 1, "DestoryCard", "结束" + "保龙仓卡（中行卡）销卡" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);

                DestoryDataClass icDDC = new DestoryDataClass();
                icDDC.CodeMsg.Code = msg.codeMsg.code;
                icDDC.CodeMsg.Msg = msg.codeMsg.msg;

                return icDDC;
            }
            else
            {
                Log2SQL(0, 1, "DestoryCard", "开始" + "保龙仓卡（中行卡）销卡,没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardNo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }
        }
        catch (Exception e)
        {
            DestoryDataClass icDDC = new DestoryDataClass();
            icDDC.CodeMsg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                icDDC.CodeMsg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                icDDC.CodeMsg.Msg = e.Message;
            }
            Log2SQL(0, 1, "DestoryCard", "开始" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardNo + "----" + CClass.CardNo + "----" + GClass.GuID);
            return icDDC;

        }
    }
    //'modify code 036:end------------------------------------------------------------------------- 

    //'modify code 047:start------------------------------------------------------------------------- 
    //记名卡非记名卡售卖充值
    [WebMethod]
    public IslvBySignUnSignData islvBySignUnSign(IslvBySignUnSignBean CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.IslvBySignUnSignBean islvBySignUnSignBean = new ImTxnWebReference.IslvBySignUnSignBean();
                islvBySignUnSignBean.merchantNo = CClass.MerchantNo;
                islvBySignUnSignBean.userId = CClass.UserID;
                islvBySignUnSignBean.totalAmount = CClass.TotalAmount;
                islvBySignUnSignBean.discountAmount = CClass.DiscountAmount;
                islvBySignUnSignBean.signFlag = CClass.SignFlag;
                islvBySignUnSignBean.cardInfos = new ImTxnWebReference.SignUnSignOrderCardInfo[CClass.OrderCardInfos.Length];

                Int16 i16Count;
                i16Count = 0;
                foreach (OrderCardInfo orderCardInfo in CClass.OrderCardInfos)
                {
                    ImTxnWebReference.SignUnSignOrderCardInfo iOrderCardInfo = new ImTxnWebReference.SignUnSignOrderCardInfo();
                    iOrderCardInfo.cardNoFrom = orderCardInfo.CardNoFrom;
                    iOrderCardInfo.cardNoTo = orderCardInfo.CardNoTo;
                    iOrderCardInfo.cardNum = orderCardInfo.CardNum;
                    iOrderCardInfo.cardAmount = orderCardInfo.CardAmount;
                    iOrderCardInfo.expiredDate = orderCardInfo.ExpiredDate;
                    islvBySignUnSignBean.cardInfos.SetValue(iOrderCardInfo, i16Count);
                    i16Count++;
                }
                if (CClass.OrderPayInfos == null)
                {
                    islvBySignUnSignBean.payInfos = null;
                }
                else
                {
                    islvBySignUnSignBean.payInfos = new ImTxnWebReference.OrderPayInfo[CClass.OrderPayInfos.Length];
                    i16Count = 0;
                    foreach (OrderPayInfo orderPayInfo in CClass.OrderPayInfos)
                    {
                        ImTxnWebReference.OrderPayInfo iOrderPayInfo = new ImTxnWebReference.OrderPayInfo();
                        iOrderPayInfo.payType = orderPayInfo.PayType;
                        iOrderPayInfo.payAmount = orderPayInfo.PayAmount;
                        iOrderPayInfo.tAccountName = orderPayInfo.AccountName;
                        iOrderPayInfo.tAccountNo = orderPayInfo.AccountNo;
                        iOrderPayInfo.bBankName = orderPayInfo.BankName;
                        iOrderPayInfo.bBankCardNo = orderPayInfo.BankCardNo;
                        iOrderPayInfo.oRemark = orderPayInfo.Remark;
                        islvBySignUnSignBean.payInfos.SetValue(iOrderPayInfo, i16Count);
                        i16Count++;
                    }
                }
                Log2SQL(0, 1, "islvBySignUnSign", "开始" + "记名卡非记名卡售卖充值" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.OrderCardInfos[0].CardNoFrom + "----" + CClass.OrderCardInfos[0].CardNoTo);
                ImTxnWebReference.IslvBySignUnSignData islvBySignUnSignData = culService.islvBySignUnSign(islvBySignUnSignBean);
                Log2SQL(0, 1, "islvBySignUnSign", "结束" + "记名卡非记名卡售卖充值" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.OrderCardInfos[0].CardNoFrom + "----" + CClass.OrderCardInfos[0].CardNoTo);
                IslvBySignUnSignData C4IslvBySignUnSignData = new IslvBySignUnSignData();
                C4IslvBySignUnSignData.Code = islvBySignUnSignData.code;
                C4IslvBySignUnSignData.Msg = islvBySignUnSignData.msg;
                C4IslvBySignUnSignData.TxnNo = islvBySignUnSignData.txnNo;

                return C4IslvBySignUnSignData;
            }
            else
            {
                Log2SQL(0, 1, "islvBySignUnSign", "开始" + "记名卡非记名卡售卖充值，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.OrderCardInfos[0].CardNoFrom + "----" + CClass.OrderCardInfos[0].CardNoTo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            IslvBySignUnSignData C4IslvBySignUnSignData = new IslvBySignUnSignData();
            C4IslvBySignUnSignData.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4IslvBySignUnSignData.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4IslvBySignUnSignData.Msg = e.Message;
            }
            Log2SQL(0, 1, "islvBySignUnSign", "开始记名卡非记名卡售卖充值" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.OrderCardInfos[0].CardNoFrom + "----" + GClass.GuID);
            return C4IslvBySignUnSignData;
        }
    }
    //记名卡非记名卡注册
    [WebMethod]
    public CodeMsg cardSignFlagRegister(CardSignFlagRegisterBean CClass, GuIDClass GClass)
    {
        try
        {
            if (Iflogin(GClass))
            {
                ImTxnWebReference.imTxnServiceWse culService = getCULService();

                ImTxnWebReference.CardSignFlagRegisterBean cardSignFlagRegisterBean = new ImTxnWebReference.CardSignFlagRegisterBean();
                cardSignFlagRegisterBean.merchantNo = CClass.MerchantNo;
                cardSignFlagRegisterBean.userId = CClass.UserID;
                cardSignFlagRegisterBean.cardSignFlagRegisterInfos = new ImTxnWebReference.CardSignFlagRegisterInfo[CClass.CardSignFlagRegisterInfos.Length];
                Int16 i16Count = 0;
                foreach (CardSignFlagRegisterInfo cardSignFlagRegisterInfo in CClass.CardSignFlagRegisterInfos)
                {
                    ImTxnWebReference.CardSignFlagRegisterInfo icardSignFlagRegisterInfo = new ImTxnWebReference.CardSignFlagRegisterInfo();
                    icardSignFlagRegisterInfo.cardNoFrom = cardSignFlagRegisterInfo.CardNoFrom;
                    icardSignFlagRegisterInfo.cardNoTo = cardSignFlagRegisterInfo.CardNoTo;
                    icardSignFlagRegisterInfo.cardNum = cardSignFlagRegisterInfo.CardNum;
                    icardSignFlagRegisterInfo.signFlag = cardSignFlagRegisterInfo.SignFlag;
                    cardSignFlagRegisterBean.cardSignFlagRegisterInfos.SetValue(icardSignFlagRegisterInfo, i16Count);
                    i16Count++;
                }



                Log2SQL(0, 1, "cardSignFlagRegister", "开始" + "记名卡非记名卡注册" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardSignFlagRegisterInfos[0].CardNoFrom + "----" + CClass.CardSignFlagRegisterInfos[0].CardNoTo);
                ImTxnWebReference.CodeMsg msg = culService.cardSignFlagRegister(cardSignFlagRegisterBean);
                Log2SQL(0, 1, "cardSignFlagRegister", "结束" + "记名卡非记名卡注册" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardSignFlagRegisterInfos[0].CardNoFrom + "----" + CClass.CardSignFlagRegisterInfos[0].CardNoTo);
                CodeMsg C4Msg = new CodeMsg();
                C4Msg.Code = msg.code;
                C4Msg.Msg = msg.msg;

                return C4Msg;
            }
            else
            {
                Log2SQL(0, 1, "cardSignFlagRegister", "开始" + "记名卡非记名卡注册，没有登录系统不能进行CUL特殊操作！" + CClass.MerchantNo + "----" + GClass.GuID + "----" + CClass.CardSignFlagRegisterInfos[0].CardNoFrom + "----" + CClass.CardSignFlagRegisterInfos[0].CardNoTo);
                throw new Exception("没有登录系统不能进行CUL特殊操作！");
            }

        }
        catch (Exception e)
        {
            CodeMsg C4Msg = new CodeMsg();
            C4Msg.Code = "NU";
            if (e.Message.Length > 2000)
            {
                C4Msg.Msg = e.Message.Substring(0, 2000);
            }
            else
            {
                C4Msg.Msg = e.Message;
            }
            Log2SQL(0, 1, "cardSignFlagRegister", "开始记名卡非记名卡注册" + e.Message.Substring(0, 2000) + CClass.MerchantNo + "----" + CClass.CardSignFlagRegisterInfos[0].CardNoFrom + "----" + GClass.GuID);
            return C4Msg;
        }
    }
    //'modify code 047:end-------------------------------------------------------------------------
}
#region
[Serializable]
public class BatchPostponeClass
{
    public BatchPostponeClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string reqExpireDate; public string ReqExpireDate { get { return reqExpireDate; } set { reqExpireDate = value; } }
}
public class CodeMsg 
{
    public CodeMsg() { }
    private string code;public string Code {get { return code; } set { code = value; } }
    private string msg;public string Msg {get { return msg; } set { msg = value; } }
}
//券反馈类
public class VCodeMsg
{
    public VCodeMsg() { }
    private string code; public string Code { get { return code; } set { code = value; } }
    private string msg; public string Msg { get { return msg; } set { msg = value; } }
    private string resId; public string ResId { get { return resId; } set { resId = value; } }
}
//卡状态操作输入类
public class StatusClass 
{ 
    public StatusClass() {}
    private string type; public string Type { get { return type; } set { type = value; } }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }

}
//券状态操作输入类
public class VStatusClass
{
    public VStatusClass() { }
    private string type; public string Type { get { return type; } set { type = value; } }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string vTypeId; public string VTypeId { get { return vTypeId; } set { vTypeId = value; } }
    private string vSeqNoFrom; public string VSeqNoFrom { get { return vSeqNoFrom; } set { vSeqNoFrom = value; } }
    private string vSeqNoTo; public string VSeqNoTo { get { return vSeqNoTo; } set { vSeqNoTo = value; } }
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }

}
//
public class IslvClass
{
    public IslvClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string amount; public string Amount { get { return amount; } set { amount = value; } }
    private string totalAmount; public string TotalAmount { get { return totalAmount; } set { totalAmount = value; } }
    private string expiredDate; public string ExpiredDate { get { return expiredDate; } set { expiredDate = value; } }
    private string discount; public string Discount { get { return discount; } set { discount = value; } }
    private string buyPerson; public string BuyPerson { get { return buyPerson; } set { buyPerson = value; } }
    private string remarks; public string Remarks { get { return remarks; } set { remarks = value; } }
}
public class IdadClass
{
    public IdadClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string rCorporateNo; public string RCorporateNo { get { return rCorporateNo; } set { rCorporateNo = value; } }
    private string rMerchantNo; public string RMerchantNo { get { return rMerchantNo; } set { rMerchantNo = value; } }
    private string amount; public string Amount { get { return amount; } set { amount = value; } }
}
public class ItrfClass
{
    public ItrfClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string password; public string Password { get { return password; } set { password = value; } }
    private string amount; public string Amount { get { return amount; } set { amount = value; } }
    private string remarks; public string Remarks { get { return remarks; } set { remarks = value; } }

}
public class IctvClass
{
    public IctvClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string password; public string Password { get { return password; } set { password = value; } }
}
public class IrecClass
{
    public IrecClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }   
}
public class IdvvClass
{
    public IdvvClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string amount; public string Amount { get { return amount; } set { amount = value; } }
    private string totalAmount; public string TotalAmount { get { return totalAmount; } set { totalAmount = value; } }
    private string dMerchantNo; public string DMerchantNo { get { return dMerchantNo; } set { dMerchantNo = value; } }
}
//券充值撤销输入类
public class VIdvvClass
{
    public VIdvvClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string vTypeId; public string VTypeId { get { return vTypeId; } set { vTypeId = value; } }
    private string vSeqNoFrom; public string VSeqNoFrom { get { return vSeqNoFrom; } set { vSeqNoFrom = value; } }
    private string vSeqNoTo; public string VSeqNoTo { get { return vSeqNoTo; } set { vSeqNoTo = value; } }
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }
    private string vNumber; public string VNumber { get { return vNumber; } set { vNumber = value; } }
    private string vAmount; public string VAmount { get { return vAmount; } set { vAmount = value; } }
    private string vTotalAmount; public string VTotalAmount { get { return vTotalAmount; } set { vTotalAmount = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }    
}
//券密码重置输入类
public class VRstpClass
{
    public VRstpClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string vTypeId; public string VTypeId { get { return vTypeId; } set { vTypeId = value; } }
    private string vSeqNoFrom; public string VSeqNoFrom { get { return vSeqNoFrom; } set { vSeqNoFrom = value; } }
    private string vSeqNoTo; public string VSeqNoTo { get { return vSeqNoTo; } set { vSeqNoTo = value; } }
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }
    private string password; public string Password { get { return password; } set { password = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }
    private string seqNo; public string SeqNo { get { return seqNo; } set { seqNo = value; } }
}

//券消费撤销输入类
public class VTxnvoidClass
{
    public VTxnvoidClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string termNo; public string TermNo { get { return termNo; } set { termNo = value; } }   
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }
    private string vAmount; public string VAmount { get { return vAmount; } set { vAmount = value; } }    
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }
    private string txnSeq; public string TxnSeq { get { return txnSeq; } set { txnSeq = value; } }
}

//卡信息查询输入类
public class InfoClass
{
    public InfoClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string isPager; public string IsPager { get { return isPager; } set { isPager = value; } }
    private string pageNo; public string PageNo { get { return pageNo; } set { pageNo = value; } }
    private string pageSize; public string PageSize { get { return pageSize; } set { pageSize = value; } }

}
//卡返回单卡类
public class Card
{
    public Card() { }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string balance; public string Balance { get { return balance; } set { balance = value; } }
    private string activedDate; public string ActivedDate { get { return activedDate; } set { activedDate = value; } }
    private string status; public string Status { get { return status; } set { status = value; } }
    private string hotReason; public string HotReason { get { return hotReason; } set { hotReason = value; } }
    private string expiredDate; public string ExpiredDate { get { return expiredDate; } set { expiredDate = value; } }
    private string issuerMerchant; public string IssuerMerchant { get { return issuerMerchant; } set { issuerMerchant = value; } }
    private string issuerCreateUser; public string IssuerCreateUser { get { return issuerCreateUser; } set { issuerCreateUser = value; } }
    private string loyaltyClub; public string LoyaltyClub { get { return loyaltyClub; } set { loyaltyClub = value; } }
}
//卡信息查询返回类
public class InfoDataClass
{
    public InfoDataClass() { codeMsg = new CodeMsg(); }
    public InfoDataClass(int cardNumber) { codeMsg = new CodeMsg(); Cards = new Card[cardNumber]; }
    private string total; public string Total { get { return total; } set { total = value; } }
    private CodeMsg codeMsg; public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }

    public Card[] Cards;
}
//券信息查询输入类
public class VInfoClass 
{
    public VInfoClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string typeId; public string TypeId { get { return typeId; } set { typeId = value; } }
    private string seqNoFrom; public string SeqNoFrom { get { return seqNoFrom; } set { seqNoFrom = value; } }
    private string seqNoTo; public string SeqNoTo { get { return seqNoTo; } set { seqNoTo = value; } }
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }   
    private string isPager; public string IsPager { get { return isPager; } set { isPager = value; } }
    private string pageNo; public string PageNo { get { return pageNo; } set { pageNo = value; } }
    private string pageSize; public string PageSize { get { return pageSize; } set { pageSize = value; } }

}
//卡返回单卡类
public class Voucher
{
    public Voucher() { }
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }
    private string seqNo; public string SeqNo { get { return seqNo; } set { seqNo = value; } }
    private string typeId; public string TypeId { get { return typeId; } set { typeId = value; } }
    private string status; public string Status { get { return status; } set { status = value; } }
    private string expiredDate; public string ExpiredDate { get { return expiredDate; } set { expiredDate = value; } }
    private string activedMer; public string ActivedDMer { get { return activedMer; } set { activedMer = value; } }
    private string activedMerName; public string ActivedMerName { get { return activedMerName; } set { activedMerName = value; } }
    private string activedDate; public string ActivedDate { get { return activedDate; } set { activedDate = value; } }
    private string useMer; public string UseMer { get { return useMer; } set { useMer = value; } }
    private string useMerName; public string UseMerName { get { return useMerName; } set { useMerName = value; } }
    private string useTime; public string UseTime { get { return useTime; } set { useTime = value; } }
    private string amount; public string Amount { get { return amount; } set { amount = value; } }
  
}
//券信息查询返回类
public class VInfoDataClass
{
    public VInfoDataClass() { vcodeMsg = new VCodeMsg(); }
    public VInfoDataClass(int voucherNumber) { vcodeMsg = new VCodeMsg(); Vouchers = new Voucher[voucherNumber]; }
    private string total; public string Total { get { return total; } set { total = value; } }
    private VCodeMsg vcodeMsg; public VCodeMsg VCodeMsg { get { return vcodeMsg; } set { vcodeMsg = value; } }

    public Voucher[] Vouchers;
}
//卡消费查询类
public class CtqyClass
{
    public CtqyClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string queryType; public string QueryType { get { return queryType; } set { queryType = value; } }
    private string dateFrom; public string DateFrom { get { return dateFrom; } set { dateFrom = value; } }
    private string dateTo; public string DateTo { get { return dateTo; } set { dateTo = value; } }
    private string isPager; public string IsPager { get { return isPager; } set { isPager = value; } }
    private string pageNo; public string PageNo { get { return pageNo; } set { pageNo = value; } }
    private string pageSize; public string PageSize { get { return pageSize; } set { pageSize = value; } }

}


//卡消费查询返回类
public class CtqyDataClass
{
    public CtqyDataClass() { codeMsg = new CodeMsg(); }
    public CtqyDataClass(int txnNumber) {codeMsg=new CodeMsg();CardTxns=new CardTxn[txnNumber]; }
    private string total; public string Total { get { return total; } set { total = value; } }
    public CardTxn[] CardTxns;
    private CodeMsg codeMsg; public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }

}
public class CardTxn
{
    public CardTxn() { }
    private string txnId; public string TxnId { get { return txnId; } set { txnId = value; } }
    private string inputChannel; public string InputChannel { get { return inputChannel; } set { inputChannel = value; } }
    private string txnCode; public string TxnCode { get { return txnCode; } set { txnCode = value; } }
    private string earnAmount; public string EarnAmount { get { return earnAmount; } set { earnAmount = value; } }
    private string redeemAmount; public string RedeemAmount { get { return redeemAmount; } set { redeemAmount = value; } }
    private string transferAmount; public string TransferAmount { get { return transferAmount; } set { transferAmount = value; } }
    private string adjustAmount; public string AdjustAmount { get { return adjustAmount; } set { adjustAmount = value; } }
    private string status; public string Status { get { return status; } set { status = value; } }
    private string txnDate; public string TxnDate { get { return txnDate; } set { txnDate = value; } }
    private string txnTime; public string TxnTime { get { return txnTime; } set { txnTime = value; } }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string termNo; public string TermNo { get { return termNo; } set { termNo = value; } }
}
//券消费查询类
public class VtqyClass
{
    public VtqyClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }
    private string termNo; public string TermNo { get { return termNo; } set { termNo = value; } }
    private string queryType; public string QueryType { get { return queryType; } set { queryType = value; } }    
    private string dateFrom; public string DateFrom { get { return dateFrom; } set { dateFrom = value; } }
    private string dateTo; public string DateTo { get { return dateTo; } set { dateTo = value; } }
    private string isPager; public string IsPager { get { return isPager; } set { isPager = value; } }
    private string pageNo; public string PageNo { get { return pageNo; } set { pageNo = value; } }
    private string pageSize; public string PageSize { get { return pageSize; } set { pageSize = value; } }
    private string typeId; public string TypeId { get { return typeId; } set { typeId = value; } }
    private string seqNo; public string SeqNo { get { return seqNo; } set { seqNo = value; } }

}


//券消费查询返回类
public class VtqyDataClass
{
    public VtqyDataClass() { vcodeMsg = new VCodeMsg(); }
    public VtqyDataClass(int txnNumber) { vcodeMsg = new VCodeMsg(); VoucherTxns = new VoucherTxn[txnNumber]; }
    private string total; public string Total { get { return total; } set { total = value; } }
    public VoucherTxn[] VoucherTxns;
    private VCodeMsg vcodeMsg; public VCodeMsg VCodeMsg { get { return vcodeMsg; } set { vcodeMsg = value; } }

}
//券交易明细类
public class VoucherTxn
{
    public VoucherTxn() { }
    private string txnId; public string TxnId { get { return txnId; } set { txnId = value; } }
    private string settleDate; public string SettleDate { get { return settleDate; } set { settleDate = value; } }
    private string merNo; public string MerNo { get { return merNo; } set { merNo = value; } }
    private string merName; public string MerName { get { return merName; } set { merName = value; } }
    private string termNo; public string TermNo { get { return termNo; } set { termNo = value; } }
    private string traceNo; public string TraceNo { get { return traceNo; } set { traceNo = value; } }
    private string batchNo; public string BatchNo { get { return batchNo; } set { batchNo = value; } }
    private string refNo; public string RefNo { get { return refNo; } set { refNo = value; } }
    private string respCode; public string RespCode { get { return respCode; } set { respCode = value; } }
    private string operId; public string OperId { get { return operId; } set { operId = value; } }
    private string operType; public string OperType { get { return operType; } set { operType = value; } }
    private string voucherNo; public string VoucherNo { get { return voucherNo; } set { voucherNo = value; } }
    private string amount; public string Amount { get { return amount; } set { amount = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string iCNo; public string ICNo { get { return iCNo; } set { iCNo = value; } }
    private string tranCode; public string TranCode { get { return tranCode; } set { tranCode = value; } }
    private string tranMode; public string TranMode { get { return tranMode; } set { tranMode = value; } }
    private string createTimestamp; public string CreateTimestamp { get { return createTimestamp; } set { createTimestamp = value; } }
    private string createUser; public string CreateUser { get { return createUser; } set { createUser = value; } }   
    
}
//卡密码重置输入类
public class RstpClass
{
    public RstpClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userId; public string UserId { get { return userId; } set { userId = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string password; public string Password { get { return password; } set { password = value; } }

}
//卡密码修改输入类
public class UpdpClass
{
    public UpdpClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userId; public string UserId { get { return userId; } set { userId = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string oldPassword; public string OldPassword { get { return oldPassword; } set { oldPassword = value; } }
    private string newPassword; public string NewPassword { get { return newPassword; } set { newPassword = value; } }

}
//ICCard
public class IcCard
{
    public IcCard() { }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string balance; public string Balance { get { return balance; } set { balance = value; } }
    private string activedDate; public string ActivedDate { get { return activedDate; } set { activedDate = value; } }
    private string status; public string Status { get { return status; } set { status = value; } }
    private string hotReason; public string HotReason { get { return hotReason; } set { hotReason = value; } }
    private string expiredDate; public string ExpiredDate { get { return expiredDate; } set { expiredDate = value; } }
    private string merchantNoName; public string MerchantNoName { get { return merchantNoName; } set { merchantNoName = value; } }
    private string statusName; public string StatusName { get { return statusName; } set { statusName = value; } }
    private string hotReasonName; public string HotReasonName { get { return hotReasonName; } set { hotReasonName = value; } }  
  
}
//IC card查询输入类
public class IcClass
{
    public IcClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string icType; public string IcType { get { return icType; } set { icType = value; } }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string isPager; public string IsPager { get { return isPager; } set { isPager = value; } }
    private string pageNo; public string PageNo { get { return pageNo; } set { pageNo = value; } }
    private string pageSize; public string PageSize { get { return pageSize; } set { pageSize = value; } }
}
//IC 卡信息查询返回类
public class IcDataClass
{
    public IcDataClass() { codeMsg = new CodeMsg(); }
    public IcDataClass(int cardNumber) { codeMsg = new CodeMsg(); Cards = new IcCard[cardNumber]; }
    private string total; public string Total { get { return total; } set { total = value; } }
    private CodeMsg codeMsg; public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
    public IcCard[] Cards;
}
//IC card销卡输入类
public class IcDestoryClass
{
    public IcDestoryClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string icType; public string IcType { get { return icType; } set { icType = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string hotReason; public string HotReason { get { return hotReason; } set { hotReason = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }
}
//IC 卡销卡返回类
public class IcDestoryDataClass
{
    public IcDestoryDataClass() { codeMsg = new CodeMsg(); }    
    private string balance; public string Balance { get { return balance; } set { balance = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }   
    private CodeMsg codeMsg; public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
}
//分销卡激活撤销输入类
public class InactiveClass
{
    public InactiveClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string ean; public string Ean { get { return ean; } set { ean = value; } }
    private string refNo; public string RefNo { get { return refNo; } set { refNo = value; } }
    private string txnTime; public string TxnTime { get { return txnTime; } set { txnTime = value; } }
    private string reqTxnDate; public string ReqTxnDate { get { return reqTxnDate; } set { reqTxnDate = value; } }
    private string reqTxnTime; public string ReqTxnTime { get { return reqTxnTime; } set { reqTxnTime = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }
    private string opeType; public string OpeType { get { return opeType; } set { opeType = value; } }
}
//分销卡激活撤销返回类
public class InactiveDataClass
{
    public InactiveDataClass() { } 
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string ean; public string Ean { get { return ean; } set { ean = value; } }
    private string cardPrice; public string CardPrice { get { return cardPrice; } set { cardPrice = value; } }
    private string saleAmount; public string SaleAmount { get { return saleAmount; } set { saleAmount = value; } }
    private string refNo; public string RefNo { get { return refNo; } set { refNo = value; } }
    private string txnTime; public string TxnTime { get { return txnTime; } set { txnTime = value; } }
    private string reqTxnDate; public string ReqTxnDate { get { return reqTxnDate; } set { reqTxnDate = value; } }
    private string reqTxnTime; public string ReqTxnTime { get { return reqTxnTime; } set { reqTxnTime = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }
    private string code; public string Code { get { return code; } set { code = value; } }
    private string msg; public string Msg { get { return msg; } set { msg = value; } }

}

//离线手工激活输入类
public class OfflineActiveClass
{
    public OfflineActiveClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string ean; public string Ean { get { return ean; } set { ean = value; } }
}
//离线手工激活返回类
public class OfflineActiveDataClass
{
    public OfflineActiveDataClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string termNo; public string TermNo { get { return termNo; } set { termNo = value; } }
    private string code; public string Code { get { return code; } set { code = value; } }
    private string msg; public string Msg { get { return msg; } set { msg = value; } }

    private string productName; public string ProductName { get { return productName; } set { productName = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string ean; public string Ean { get { return ean; } set { ean = value; } }

    private string txnDate; public string TxnDate { get { return txnDate; } set { txnDate = value; } }
    private string txnTime; public string TxnTime { get { return txnTime; } set { txnTime = value; } }
    private string txnType; public string TxnType { get { return txnType; } set { txnType = value; } }

    private string posBatchNo; public string PosBatchNo { get { return posBatchNo; } set { posBatchNo = value; } }
    private string posRefNo; public string PosRefNo { get { return posRefNo; } set { posRefNo = value; } }
    private string retriRefNo; public string RetriRefNo { get { return retriRefNo; } set { retriRefNo = value; } }
    private string cardPrice; public string CardPrice { get { return cardPrice; } set { cardPrice = value; } }
    private string salePrice; public string SalePrice { get { return salePrice; } set { salePrice = value; } }
    private string manageCard; public string ManageCard { get { return manageCard; } set { manageCard = value; } }
    
}
//对一张分销卡片进行充值操作 输入类
//String merchantNo：分店编号，15位，非空
//String userId：操作员代码，20位，非空
//String cardNo：卡号，19位，非空
//String ean: 商品条码，13位，非空
//String salePrice: 销售价，可空(单位圆)
//String reqTxnDate: 请求日期，YYYYMMDD，可空
//String reqTxnTime: 请求时间，HHmmss，可空
//String reqId: 请求流水号，20位数字，可空
public class ActiveClass
{
    public ActiveClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string ean; public string Ean { get { return ean; } set { ean = value; } }
    private string salePrice; public string SalePrice { get { return salePrice; } set { salePrice = value; } }
    private string reqTxnDate; public string ReqTxnDate { get { return reqTxnDate; } set { reqTxnDate = value; } }
    private string reqTxnTime; public string ReqTxnTime { get { return reqTxnTime; } set { reqTxnTime = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }
}
//对一张分销卡片进行充值操作 返回类
//String code：错误代码，2位
//String msg：错误信息，20位
//String merchantNo：分店编号，15位，非空
//String userId：操作员代码，20位，非空
//String cardNo：卡号，19位，非空
//String ean: 商品条码，13位，非空
//String salePrice: 销售价，可空
//String resTxnDate: 返回日期，YYYYMMDD
//String resTxnTime: 返回时间，HHmmss
//String resId: 返回流水号，20位（本次接口中不返回）
public class ActiveDataClass
{
    public ActiveDataClass() { }
    private string code; public string Code { get { return code; } set { code = value; } }
    private string msg; public string Msg { get { return msg; } set { msg = value; } }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string ean; public string Ean { get { return ean; } set { ean = value; } }
    private string salePrice; public string SalePrice { get { return salePrice; } set { salePrice = value; } }
    private string reqTxnDate; public string ReqTxnDate { get { return reqTxnDate; } set { reqTxnDate = value; } }
    private string reqTxnTime; public string ReqTxnTime { get { return reqTxnTime; } set { reqTxnTime = value; } }
    private string reqId; public string ReqId { get { return reqId; } set { reqId = value; } }

}
//传递GuID验证参数
public class GuIDClass
{
    public GuIDClass() { }
    private string guID; public string GuID { get { return guID; } set { guID = value; } }
}

//'modify code 036:start------------------------------------------------------------------------- 
//保龙仓卡（中行卡）查询输入类
public class M27j27CardInfoClass
{
    public M27j27CardInfoClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string isPager; public string IsPager { get { return isPager; } set { isPager = value; } }
    private string pageNo; public string PageNo { get { return pageNo; } set { pageNo = value; } }
    private string pageSize; public string PageSize { get { return pageSize; } set { pageSize = value; } }

}
//保龙仓卡（中行卡）交易明细查询输入类
public class M27j27CardTxnClass
{
    public M27j27CardTxnClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string queryType; public string QueryType { get { return queryType; } set { queryType = value; } }
    private string dateFrom; public string DateFrom { get { return dateFrom; } set { dateFrom = value; } }
    private string dateTo; public string DateTo { get { return dateTo; } set { dateTo = value; } }
    private string isPager; public string IsPager { get { return isPager; } set { isPager = value; } }
    private string pageNo; public string PageNo { get { return pageNo; } set { pageNo = value; } }
    private string pageSize; public string PageSize { get { return pageSize; } set { pageSize = value; } }
}
//保龙仓卡（中行卡）销卡输入类
public class DestoryClass
{
    public DestoryClass() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string cardNo; public string CardNo { get { return cardNo; } set { cardNo = value; } }
    private string hotReason; public string HotReason { get { return hotReason; } set { hotReason = value; } }
}
//保龙仓卡（中行卡）销卡返回类
public class DestoryDataClass
{
    public DestoryDataClass() { codeMsg = new CodeMsg(); }
    private CodeMsg codeMsg; public CodeMsg CodeMsg { get { return codeMsg; } set { codeMsg = value; } }
}
//'modify code 036:end------------------------------------------------------------------------- 

//'modify code 047:start------------------------------------------------------------------------- 
//记名卡非记名卡售卖充值输入类
public class IslvBySignUnSignBean
{
    public IslvBySignUnSignBean() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private string totalAmount; public string TotalAmount { get { return totalAmount; } set { totalAmount = value; } }
    private string discountAmount; public string DiscountAmount { get { return discountAmount; } set { discountAmount = value; } }
    private string signFlag; public string SignFlag { get { return signFlag; } set { signFlag = value; } }
    private OrderCardInfo[] orderCardInfos; public OrderCardInfo[] OrderCardInfos { get { return orderCardInfos; } set { orderCardInfos = value; } }
    private OrderPayInfo[] orderPayInfos; public OrderPayInfo[] OrderPayInfos { get { return orderPayInfos; } set { orderPayInfos = value; } }
}
//记名卡非记名卡售卖充值购卡信息输入类
public class OrderCardInfo
{
    public OrderCardInfo() { }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string cardNum; public string CardNum { get { return cardNum; } set { cardNum = value; } }
    private string cardAmount; public string CardAmount { get { return cardAmount; } set { cardAmount = value; } }
    private string expiredDate; public string ExpiredDate { get { return expiredDate; } set { expiredDate = value; } }
}
//记名卡非记名卡售卖充值支付信息输入类
public class OrderPayInfo
{
    public OrderPayInfo() { }
    private string payType; public string PayType { get { return payType; } set { payType = value; } }
    private string payAmount; public string PayAmount { get { return payAmount; } set { payAmount = value; } }
    private string accountName; public string AccountName { get { return accountName; } set { accountName = value; } }
    private string accountNo; public string AccountNo { get { return accountNo; } set { accountNo = value; } }
    private string bankName; public string BankName { get { return bankName; } set { bankName = value; } }
    private string bankCardNo; public string BankCardNo { get { return bankCardNo; } set { bankCardNo = value; } }
    private string remark; public string Remark { get { return remark; } set { remark = value; } }
}
//记名卡非记名卡售卖充值返回类
public class IslvBySignUnSignData
{
    public IslvBySignUnSignData() { }
    private string code; public string Code { get { return code; } set { code = value; } }
    private string msg; public string Msg { get { return msg; } set { msg = value; } }
    private string txnNo; public string TxnNo { get { return txnNo; } set { txnNo = value; } }
}
//记名卡非记名卡注册输入类
public class CardSignFlagRegisterBean
{
    public CardSignFlagRegisterBean() { }
    private string merchantNo; public string MerchantNo { get { return merchantNo; } set { merchantNo = value; } }
    private string userID; public string UserID { get { return userID; } set { userID = value; } }
    private CardSignFlagRegisterInfo[] cardSignFlagRegisterInfos; public CardSignFlagRegisterInfo[] CardSignFlagRegisterInfos { get { return cardSignFlagRegisterInfos; } set { cardSignFlagRegisterInfos = value; } }
}
//记名卡非记名卡注册信息输入类
public class CardSignFlagRegisterInfo
{
    public CardSignFlagRegisterInfo() { }
    private string cardNoFrom; public string CardNoFrom { get { return cardNoFrom; } set { cardNoFrom = value; } }
    private string cardNoTo; public string CardNoTo { get { return cardNoTo; } set { cardNoTo = value; } }
    private string cardNum; public string CardNum { get { return cardNum; } set { cardNum = value; } }
    private string signFlag; public string SignFlag { get { return signFlag; } set { signFlag = value; } }
}
//'modify code 047:end------------------------------------------------------------------------- 
#endregion