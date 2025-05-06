Imports System.Net

Imports System.Net.Sockets

Imports System.IO

Imports System.Threading

Imports System.Drawing



Public Class Form1

    Private imgClient As TcpClient

    Private ctrlClient As TcpClient



    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        imgClient = New TcpClient(txtIP.Text, 9000)
        ctrlClient = New TcpClient(txtIP.Text, 9001)

        ThreadPool.QueueUserWorkItem(AddressOf ReceiveImages)

        ' Hook up mouse events on the PictureBox
        AddHandler picRemote.MouseMove, AddressOf PicRemote_MouseMove
        AddHandler picRemote.MouseClick, AddressOf PicRemote_MouseClick
    End Sub




    Private Sub ReceiveImages(state As Object)

        Dim ns = imgClient.GetStream()

        Dim br As New BinaryReader(ns)

        Try

            While True

                Dim len = IPAddress.NetworkToHostOrder(br.ReadInt32())

                Dim data = br.ReadBytes(len)

                Using ms As New MemoryStream(data)

                    Dim bmp = CType(Bitmap.FromStream(ms), Bitmap)

                    picRemote.Invoke(Sub() picRemote.Image = New Bitmap(bmp))

                End Using

            End While

        Catch ex As Exception

            ' disconnected

        End Try

    End Sub



    Private Sub PicRemote_MouseMove(sender As Object, e As MouseEventArgs)

        SendControl(1, e.X, e.Y)

    End Sub



    Private Sub PicRemote_MouseClick(sender As Object, e As MouseEventArgs)

        SendControl(2, e.X, e.Y)

    End Sub



    Private Sub SendControl(evt As Byte, x As Integer, y As Integer)

        If ctrlClient Is Nothing OrElse Not ctrlClient.Connected Then Return

        Dim ns = ctrlClient.GetStream()

        Dim bw As New BinaryWriter(ns)

        bw.Write(evt)

        bw.Write(IPAddress.HostToNetworkOrder(x))

        bw.Write(IPAddress.HostToNetworkOrder(y))

        bw.Flush()

    End Sub

End Class