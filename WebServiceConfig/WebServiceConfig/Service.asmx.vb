Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel

' 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
' <System.Web.Script.Services.ScriptService()> _
<System.Web.Services.WebService(Namespace:="http://tempuri.org/")> _
<System.Web.Services.WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<ToolboxItem(False)> _
Public Class Service
    Inherits System.Web.Services.WebService

    Private sTrainingWebService As String = ConfigurationSettings.AppSettings("sTrainingWebService")
    Private sProducingWebService As String = ConfigurationSettings.AppSettings("sProducingWebService")
    Private sTrainingConnection As String = ConfigurationSettings.AppSettings("sTrainingConnection")
    Private sProducingConnection As String = ConfigurationSettings.AppSettings("sProducingConnection")
    Private sReportConnection As String = ConfigurationSettings.AppSettings("sReportConnection")
    Private sShoppingCardWebService As String = ConfigurationSettings.AppSettings("sShoppingCardWebService")

    Private sCulReportConnection As String = ConfigurationSettings.AppSettings("sCulReportConnection")
    Private sCULReportWeb As String = ConfigurationSettings.AppSettings("sCULReportWeb")
    Private sLoginShowMsg As String = ConfigurationSettings.AppSettings("sLoginShowMsg")

    Private sUpdateSystemConnection As String = ConfigurationSettings.AppSettings("sUpdateSystemConnection")

    Private sInternetSalesWebService As String = ConfigurationSettings.AppSettings("sInternetSalesWebService")

    <WebMethod()> _
    Public Function HelloWorld() As String
        Return "Hello World"
    End Function

    <WebMethod()> _
    Public Function GetConnection() As DataTable
        'Dim dtConnection As New DataTable
        'dtConnection.Columns.Add("sTrainingWebService", System.Type.GetType("System.String"))
        'dtConnection.Columns.Add("sProducingWebService", System.Type.GetType("System.String"))
        'dtConnection.Columns.Add("sTrainingConnection", System.Type.GetType("System.String"))
        'dtConnection.Columns.Add("sProducingConnection", System.Type.GetType("System.String"))
        'dtConnection.Columns.Add("sReportConnection", System.Type.GetType("System.String"))
        'dtConnection.Columns.Add("sShoppingCardWebService", System.Type.GetType("System.String"))
        'dtConnection.Columns.Add("sCulReportConnection", System.Type.GetType("System.String"))
        'dtConnection.Columns.Add("sCULReportWeb", System.Type.GetType("System.String"))
        'dtConnection.Rows.Add(sTrainingWebService, sProducingWebService, sTrainingConnection, sProducingConnection, sReportConnection, sShoppingCardWebService, sCulReportConnection, sCULReportWeb)
        'dtConnection.TableName = "dtConnection"
        'dtConnection.AcceptChanges()
        'Return dtConnection
        Dim dtConnection As New DataTable
        Dim arr As ArrayList = New ArrayList()
        For Each key As String In ConfigurationManager.AppSettings.AllKeys
            dtConnection.Columns.Add(key, System.Type.GetType("System.String"))
            arr.Add(ConfigurationManager.AppSettings(key))
        Next
        dtConnection.Rows.Add(arr.ToArray())
        dtConnection.TableName = "dtConnection"
        dtConnection.AcceptChanges()
        Return dtConnection
    End Function

    <WebMethod()> _
    Public Function GetUpdateSystemConnection() As DataTable
        Dim dtUpdateSystemConnection As New DataTable
        dtUpdateSystemConnection.Columns.Add("sUpdateSystemConnection", System.Type.GetType("System.String"))
        dtUpdateSystemConnection.Rows.Add(sUpdateSystemConnection)
        dtUpdateSystemConnection.TableName = "dtUpdateSystemConnection"
        dtUpdateSystemConnection.AcceptChanges()
        Return dtUpdateSystemConnection
    End Function

    <WebMethod()> _
    Public Function GetLoginShowMsg() As DataTable
        Dim dtLoginShowMsg As New DataTable
        dtLoginShowMsg.Columns.Add("sLoginShowMsg", System.Type.GetType("System.String"))
        dtLoginShowMsg.Rows.Add(sLoginShowMsg)
        dtLoginShowMsg.TableName = "dtLoginShowMsg"
        dtLoginShowMsg.AcceptChanges()
        Return dtLoginShowMsg
    End Function

End Class