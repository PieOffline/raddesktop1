Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Public Class Form1

    Private imgListener As TcpListener
    Private ctrlListener As TcpListener
    Private clientImg As TcpClient
    Private clientCtrl As TcpClient

    ' P/Invoke for mouse injection
    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Sub mouse_event(dwFlags As Integer, dx As Integer, dy As Integer, cButtons As Integer, dwExtraInfo As Integer)
    End Sub

    Const MOUSEEVENTF_MOVE As Integer = &H1
    Const MOUSEEVENTF_LEFTDOWN As Integer = &H2
    Const MOUSEEVENTF_LEFTUP As Integer = &H4

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Listen on port 9000 for images, 9001 for controls
        imgListener = New TcpListener(IPAddress.Any, 9000)
        ctrlListener = New TcpListener(IPAddress.Any, 9001)
        imgListener.Start()
        ctrlListener.Start()

        ThreadPool.QueueUserWorkItem(AddressOf AcceptImageClient)
        ThreadPool.QueueUserWorkItem(AddressOf AcceptControlClient)

        ' Timer: capture & broadcast screen every 100 ms
        Dim t As New Timer(AddressOf BroadcastScreen, Nothing, 0, 100)
    End Sub

    Private Sub AcceptImageClient(state As Object)
        clientImg = imgListener.AcceptTcpClient()
    End Sub

    Private Sub AcceptControlClient(state As Object)
        clientCtrl = ctrlListener.AcceptTcpClient()
        Dim ns = clientCtrl.GetStream()
        Dim br As New BinaryReader(ns)
        Try
            While True
                Dim evt = br.ReadByte() ' 1=move,2=click
                Dim x = IPAddress.NetworkToHostOrder(br.ReadInt32())
                Dim y = IPAddress.NetworkToHostOrder(br.ReadInt32())
                If evt = 1 Then
                    mouse_event(MOUSEEVENTF_MOVE, x, y, 0, 0)
                ElseIf evt = 2 Then
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
                End If
            End While
        Catch ex As Exception
            ' client disconnected
        End Try
    End Sub

    Private Sub BroadcastScreen(state As Object)
        If clientImg Is Nothing OrElse Not clientImg.Connected Then Return
        Try
            Dim screenBounds = Screen.PrimaryScreen.Bounds
            Using bmp As New Bitmap(screenBounds.Width, screenBounds.Height)
                Using g = Graphics.FromImage(bmp)
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size)
                End Using

                Using ms As New MemoryStream()
                    bmp.Save(ms, ImageFormat.Jpeg) ' you can tweak quality here
                    Dim data = ms.ToArray()
                    Dim ns = clientImg.GetStream()
                    Dim bw As New BinaryWriter(ns)
                    bw.Write(IPAddress.HostToNetworkOrder(data.Length))
                    bw.Write(data)
                    bw.Flush()
                End Using
            End Using
        Catch ex As Exception
            clientImg.Close()
            clientImg = Nothing
        End Try
    End Sub

End Class
