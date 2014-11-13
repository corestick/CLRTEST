using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using Tobe_LOG;
using System.Threading;

/// <summary>
/// XPushProvider
/// 유종원
/// 14. 11. 13
/// </summary>
public class XPushProvider
{

    // 확장아스키코드사용
    private Encoding exASCIIENC = Encoding.GetEncoding(28591);

    //Log
    private ActLog comActLog = new ActLog();

    IPEndPoint serverAddress;
    Socket providerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    

    private static Char COL_SEP = (char)0x11; // 데이터간 구분자
    private static Char TERM_CODE = (char)0xff; // 패킷 종료 문자

    private string id = "id";
    private string pw = "pw";

    private class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    // ManualResetEvent instances signal completion.
    private static ManualResetEvent connectDone = new ManualResetEvent(false);
    private static ManualResetEvent sendDone = new ManualResetEvent(false);
    private static ManualResetEvent receiveDone = new ManualResetEvent(false);

    public XPushProvider()
    {
        
    }

    public XPushProvider(string id, string pw)
    {
        this.id = id;
        this.pw = pw;
    }

    private void connectSocket()
    {
        try
        {
            //IP, PortNo (webconfig)
            string ipAddress = ConfigurationSettings.AppSettings["XPushProviderIP"].ToString();
            int portNo = Convert.ToInt16(ConfigurationSettings.AppSettings["XPushProviderPort"].ToString());
            serverAddress = new IPEndPoint(IPAddress.Parse(ipAddress), portNo);

            // 소켓 연결
            providerSocket.BeginConnect(serverAddress, new AsyncCallback(ConnectCallback), providerSocket);
            connectDone.WaitOne();
        }
        catch (SocketException se)
        {
            comActLog.ExceptionLogWrite("[SocketException]" + se.ToString());

        }
        catch (Exception e)
        {
            comActLog.ExceptionLogWrite("[Exception]" + e.ToString());
        }
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.
            client.EndConnect(ar);
         
            comActLog.LogWrite("[XPUSHPROVIDER:CONNECT]Socket connected to " + client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.
            connectDone.Set();
        }
        catch (Exception e)
        {
            comActLog.ExceptionLogWrite("[SocketException:CONNECT]" + e.ToString());
        }
    } 

    private void sendMsg(string sendMsg)
    {
        try
        {
            // 메시지 전송
            // Send test data to the remote device.
            Send(providerSocket, sendMsg);
            sendDone.WaitOne();

            // 메시지 수신
            // Receive the response from the remote device.
            Receive(providerSocket);
            receiveDone.WaitOne();
        }
        catch (SocketException se)
        {
            comActLog.ExceptionLogWrite("[SocketException]" + se.ToString());
        }
        catch (Exception e)
        {
            comActLog.ExceptionLogWrite("[Exception]" + e.ToString());
        }
    }

    private void Send(Socket client, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = exASCIIENC.GetBytes(data);

        //테스트용 AUTHidpw
        //30-30-30-30-31-30-41-55-54-48-69-64-11-70-77-FF
        string hex = BitConverter.ToString(byteData);
        comActLog.LogWrite(hex);
        //---------
      
        // Begin sending the data to the remote device.
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);

            comActLog.LogWrite("[XPUSHPROVIDER:SEND]Sent " + bytesSent + " bytes to server.");
            
            // Signal that all bytes have been sent.
            sendDone.Set();
        }
        catch (Exception e)
        {
            comActLog.ExceptionLogWrite("[Exception]" + e.ToString());
        }
    }

    private void Receive(Socket client)
    {
        try
        {
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            comActLog.ExceptionLogWrite("[Exception]" + e.ToString());
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(exASCIIENC.GetString(state.buffer, 0, bytesRead));

                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                // All the data has arrived; put it in response.
                if (state.sb.Length > 1)
                {
                    String response = state.sb.ToString();

                    comActLog.LogWrite("[XPUSHPROVIDER:RECEIVE]" + response);
                }
                // Signal that all bytes have been received.
                receiveDone.Set();
            }
        }
        catch (Exception e)
        {
            comActLog.ExceptionLogWrite("[Exception]" + e.ToString());
        }
    }


    private string getAuthMsg()
    {
        string authMsg = "AUTH" + this.id + COL_SEP + this.pw + TERM_CODE;
        string size = ((authMsg).Length).ToString().PadLeft(6, '0');

        return size + authMsg;
    }

    private string getPushMsg(string type, string key, string value)
    {
        string pushMsg = "PUSH" + type + COL_SEP + key + COL_SEP + value + TERM_CODE;
        string size = ((pushMsg).Length).ToString().PadLeft(6, '0');

        return size + pushMsg;
    }

    private void sendAuth()
    {
        string authMsg = getAuthMsg();
        comActLog.LogWrite("[XPUSHPROVIDER:SEND]" + authMsg);

        sendMsg(authMsg);
    }

    private void sendPush(string type, string key, string value)
    {
        string pushMsg = getPushMsg(type, key, value);
        comActLog.LogWrite("[XPUSHPROVIDER:SEND]" + pushMsg);

        sendMsg(pushMsg);
    }

    private void sendBye()
    {
        string byeMsg = "BYEC" + TERM_CODE;
        string size = ((byeMsg).Length).ToString().PadLeft(6, '0');

        sendMsg(byeMsg);
    }
    //--------------------
    public void connect()
    {
        connectSocket();
        sendAuth();
    }

    public void push(string type, string key, string value)
    {
        sendPush(type, key, value);
    }

    public void shutdown()
    {
        sendBye();

        providerSocket.Shutdown(SocketShutdown.Both);
        providerSocket.Close();
    }
}
