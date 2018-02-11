Imports System.Security.Cryptography
Imports System.Text
Imports System.IO

Module SecurityText

    Private textConverter As New UnicodeEncoding, myRijndael As New RijndaelManaged()
    Private Keys() As Byte = {233, 211, 5, 222, 9, 55, 124, 64, 234, 45, 80, 81, 24, 70, 80, 102, 205, 8, 99, 234, 67, 87, 90, 201, 22, 13, 15, 33, 89, 30, 26, 36}
    Private IVs() As Byte = {67, 38, 49, 98, 76, 112, 245, 211, 31, 17, 8, 7, 4, 12, 21, 255}

    Public Function EncryptData(ByVal sSource As String) As String
        If sSource = "" Then Return ""
        Dim encryptor As ICryptoTransform = myRijndael.CreateEncryptor(Keys, IVs)
        Dim msEncrypt As New MemoryStream()
        Dim csEncrypt As New CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)
        Dim toEncrypt() As Byte = textConverter.GetBytes(sSource)
        csEncrypt.Write(toEncrypt, 0, toEncrypt.Length)
        csEncrypt.FlushFinalBlock()

        Return Convert.ToBase64String(msEncrypt.ToArray())
    End Function

    Public Function DecryptData(ByVal sEncrypted As String) As String
        Try
            Dim Encrypted() As Byte = Convert.FromBase64String(sEncrypted)
            Dim decryptor As ICryptoTransform = myRijndael.CreateDecryptor(Keys, IVs)
            Dim msDecrypt As New MemoryStream(Encrypted)
            Dim csDecrypt As New CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)
            Dim fromEncrypted() As Byte = New Byte(Encrypted.Length - 1) {}
            csDecrypt.Read(fromEncrypted, 0, fromEncrypted.Length)

            Dim Decrypted As String = textConverter.GetString(fromEncrypted), iChar As Integer = Decrypted.Length - 1
            Do While iChar > 0
                If Asc(Decrypted.Substring(iChar, 1)) = 0 Then
                    iChar = iChar - 1
                Else
                    Exit Do
                End If
            Loop
            Decrypted = Decrypted.Substring(0, iChar + 1)
            Return Decrypted
        Catch
            Return ""
        End Try
    End Function
End Module