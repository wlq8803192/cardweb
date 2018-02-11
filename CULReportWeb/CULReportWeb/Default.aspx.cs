using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.IO;
using System.Xml;
using System.Configuration;

namespace CULReportWeb
{
    public partial class _Default : System.Web.UI.Page
    {
        public string strCULReportFolder ="";
        public string strCULReportUrl = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            strCULReportFolder = ConfigurationManager.AppSettings["CULReportFolder"];
            strCULReportUrl = ConfigurationManager.AppSettings["CULReportURL"];

            string strDate = HttpContext.Current.Request.Form["QueryDate"];
            string strCorporatNo = HttpContext.Current.Request.Form["CorporatNo"];
            string strMerchantNo = HttpContext.Current.Request.Form["MerchantNo"];
            string strsIssuerId = HttpContext.Current.Request.Form["IssuerId"];
            string strStoreName = HttpContext.Current.Request.Form["strStoreName"];
            string strDept = HttpContext.Current.Request.Form["Dept"];


            //string strDate = "20140731";
            //string strCorporatNo = "086021541100411";
            //string strMerchantNo = "102210054110421";
            //string strsIssuerId = "C020";
            //string strStoreName = "家乐福中国区";
            //string strDept = "Store";

            string strTitle = "";
            string strSelectDate="";
            string strDataType="Daily";
            
            if (strDate == null)
                return;
            string strCorporatName = strMerchantNo == "0" ? strCorporatNo : strMerchantNo;
            if (strDate.Length == 6)
            {
                string startDate = strDate.Substring(0, 4) + "/" + strDate.Substring(4, 2) + "/01";
                string endDate = DateTime.Parse(startDate).AddMonths(1).AddDays(-1).ToString("yyyy/MM/dd");
                strSelectDate=DateTime.Parse(endDate).ToString("yyyyMMdd");
                strDataType="Monthly";               
                strCorporatName += "-" + strStoreName;
                strTitle = string.Format("月报表 Monthly Report: 日期：{0}-{1} 公司：{2}", startDate, endDate, strCorporatName);
            }
            else
            {
                strCorporatName += "-" + strStoreName;
                strSelectDate=strDate;
                strDataType="Daily";
                strTitle = string.Format("日报表 Daily Report: 日期：{0} 公司：{1}", strDate, strCorporatName);
            }
            divTitle.InnerHtml = strTitle;
            divTitle.InnerHtml += "</Br><hr/>";

            DataSet ds = new DataSet();
            ds = ReadXML();
            string strfilter = string.Format("DateType='{0}' And Dept='{1}'", strDataType, strDept);
            string strsort="ReportType,Report_ID";
            DataRow[] rows = ds.Tables[0].Select(strfilter,strsort);
            string LastSubTitle = "";

            foreach (DataRow dr in rows)
            {
                string fileName=dr["FileName"].ToString();
                string strKey="{IssuerId}";
                if(fileName.IndexOf(strKey)>0)
                    fileName=fileName.Replace(strKey,strsIssuerId);
                strKey="{CorporatNo}";
                if(fileName.IndexOf(strKey)>0)
                    fileName=fileName.Replace(strKey,strCorporatNo);
                strKey="{MerchantNo}";
                if(fileName.IndexOf(strKey)>0)
                    fileName=fileName.Replace(strKey,strMerchantNo);
                string strLocalPath = string.Format(@"{0}\{1}\{2}\{3}", strCULReportFolder, strSelectDate,strCorporatNo,fileName);
                string strUrlPath = string.Format(@"{0}\{1}\{2}\{3}", strCULReportUrl, strSelectDate, strCorporatNo, fileName);

                decimal fileSize=getFileSize(strLocalPath);
                if (fileSize > -1)
                {
                    string strSubTitleTemp = dr["ReportType"].ToString();
                    if (LastSubTitle != strSubTitleTemp)
                    {
                        LastSubTitle = strSubTitleTemp;
                        divReportList.InnerHtml += "</Br>";
                        divReportList.InnerHtml += LastSubTitle;
                        divReportList.InnerHtml += "</Br>";
                    }
                    string strLine = string.Format("{0}:&nbsp;&nbsp;<a href=\"{1}\" target=\"_blank\">{2}</a>&nbsp;&nbsp;({3}KB)", dr["Name"].ToString(), strUrlPath, fileName, fileSize);
                    divReportList.InnerHtml += strLine;
                    divReportList.InnerHtml += "</Br>";
                }
            }
        }
        /// <summary>
        /// 读取CULReportConfig文件
        /// </summary>
        /// <returns></returns>
        private DataSet ReadXML()
        {
            DataSet ds = new DataSet();
            string url = Server.MapPath("CulReportConfig.xml");//获得当前文件夹下的XML文件
            StreamReader sRead = new StreamReader(url, System.Text.Encoding.UTF8);
            //以一种特定的编码从字节流读取字符,必须要转化成GB2312读取才不能出乱码
            XmlDataDocument datadoc = new XmlDataDocument();//操作XML文档
            datadoc.DataSet.ReadXml(sRead);//将读取的字节流存到DataSet里面去
            ds = datadoc.DataSet;
            datadoc = null;//清空对XML数据的操作
            sRead.Close();//关闭字节流的读取
            return ds;
        }
        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="strFullName"></param>
        /// <returns></returns>
        private decimal getFileSize(string strFullName)
        {
            decimal decSize = -1M;
            FileInfo file = new FileInfo(strFullName);
            if(file.Exists)
               decSize=Math.Round(decimal.Parse(file.Length.ToString())/1024,2);
            return decSize;
        }
        
    }
}