Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel

Imports System
Imports System.Data
Imports System.Configuration
Imports System.Data.SqlClient

' 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
' <System.Web.Script.Services.ScriptService()> _
<System.Web.Services.WebService(Namespace:="http://ShoppingCardWebService/")> _
<System.Web.Services.WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<ToolboxItem(False)> _
Public Class ReportWebService
    Inherits System.Web.Services.WebService
    Implements IDisposable

    '<WebMethod()> _
    'Public Function HelloWorld() As String
    '    Return "Hello World"
    'End Function

    Private IsConnected As Boolean = True
    Private IsConnectedReason As String = ""
    Private SqlConn As SqlConnection = Nothing

    Private sConString As String = ConfigurationSettings.AppSettings("sConString")

    Private Sub Open()
        If SqlConn Is Nothing Then
            SqlConn = New SqlConnection(sConString)
        End If
        If SqlConn.State = ConnectionState.Closed Then
            Try
                SqlConn.Open()
            Catch ex As Exception
                IsConnected = False
                IsConnectedReason = "系统连接不到数据库！"
            End Try
        End If
    End Sub

    Private Sub Close()
        If SqlConn IsNot Nothing Then
            SqlConn.Close()
            Me.Dispose()
        End If
    End Sub

    <WebMethod()> _
    Public Function GetDataTable(ByVal sSQLString As String) As DataTable
        Dim ReturnDT As DataTable = Nothing

        Try
            '连接数据库
            Open()
            '查询数据
            If IsConnected = True Then
                Dim dsSQL As DataSet = New DataSet
                Dim daSQL As SqlDataAdapter = New SqlDataAdapter(sSQLString, SqlConn)
                daSQL.SelectCommand.CommandTimeout = 300 '五分钟
                daSQL.Fill(dsSQL)
                ReturnDT = dsSQL.Tables(0)
            Else
                ReturnDT = Nothing
            End If
            '关闭数据库连接
            Close()
            '返回结果
            Return ReturnDT
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    <WebMethod()> _
    Public Function ModifyTable(ByVal sSQLString As String) As Integer
        Dim ReturnInt As Integer

        Try
            '连接数据库
            Open()
            '修改数据
            If IsConnected = True Then
                Dim sqlComm As New SqlCommand(sSQLString, SqlConn)
                sqlComm.CommandTimeout = 300 '五分钟
                Dim lReturn As Long = sqlComm.ExecuteNonQuery()
                sqlComm.Dispose()
                ReturnInt = IIf(lReturn = -1, 0, lReturn) '当执行成功但返回值为 -1 时重置为 0
            Else
                ReturnInt = -1
            End If
            '关闭数据库连接
            Close()
            '返回结果
            Return ReturnInt
        Catch ex As Exception
            Return -1
        End Try
    End Function

    <WebMethod()> _
    Public Function BulkCopyTable(ByVal sTargetTable As String, ByVal dtSourceTable As DataTable) As Int16
        Dim ReturnInt As Int16

        Try
            '连接数据库
            Open()
            '修改数据
            If IsConnected = True Then
                Dim BulkCopy As New SqlBulkCopy(SqlConn)
                BulkCopy.DestinationTableName = sTargetTable
                BulkCopy.BulkCopyTimeout = 300 '五分钟
                BulkCopy.BatchSize = 2000
                BulkCopy.NotifyAfter = dtSourceTable.Rows.Count
                BulkCopy.WriteToServer(dtSourceTable)
                dtSourceTable.Dispose()
                BulkCopy.Close()
                ReturnInt = 0
            Else
                ReturnInt = -1
            End If
            '关闭数据库连接
            Close()
            '返回结果
            Return ReturnInt
        Catch ex As Exception
            Return -1
        End Try
    End Function

End Class