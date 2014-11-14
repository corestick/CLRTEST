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
using System.IO;

/// <summary>
/// XPushProvider
/// 14. 11. 14
/// 유종원
/// </summary>
public class XPushProvider
{
    // UTF-8 사용
    Encoding utf8ENC = Encoding.GetEncoding("UTF-8");

    IPEndPoint serverAddress;
    Socket providerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    ActLog comActLog = new ActLog();

    private static Char COL_SEP = (char)0x11; // 데이터간 구분자
    private static Char TERM_CODE = (char)0xff; // 패킷 종료 문자

    private static string ACTIONTYPE_AUTH = "AUTH";
    private static string ACTIONTYPE_PUSH = "PUSH";
    private static string ACTIONTYPE_BYEC = "BYEC";

    private string id = "id";
    private string pw = "pw";

    public XPushProvider()
    {
        //
        // TODO: 여기에 생성자 논리를 추가합니다.
        //
    }

    private void connectSocket()
    {
        try
        {
            string ipAddress = ConfigurationSettings.AppSettings["XPushProviderIP"].ToString();
            int portNo = Convert.ToInt16(ConfigurationSettings.AppSettings["XPushProviderPort"].ToString());
            serverAddress = new IPEndPoint(IPAddress.Parse(ipAddress), portNo);

            // 소켓 연결
            providerSocket.Connect(serverAddress);
            comActLog.LogWrite("[XPUSHPROVIDER:CONNECT]Connect");
        }
        catch (SocketException se)
        {
            comActLog.ExceptionLogWrite("[SocketException:CONNECT]" + se.Message);
        }
        catch (Exception e)
        {
            comActLog.ExceptionLogWrite("[Unexpected exception:CONNECT]" + e.Message);
        }
    }

    public XPushProvider(string id, string pw)
    {
        this.id = id;
        this.pw = pw;
    }

    private string sendAuth()
    {
        try
        {
            string authMsg = ACTIONTYPE_AUTH + this.id + COL_SEP + this.pw;
            string recMsg = sendMsg(getXpushPacket(authMsg));

            return recMsg;
        }
        catch (Exception e)
        {
            string strMessage = "[Unexpected exception]" + e.Message;
            comActLog.ExceptionLogWrite(strMessage);

            return null;
        }
    }

    private string sendPush(string type, string key, string value)
    {
        try
        {
            string pushMsg = ACTIONTYPE_PUSH + type + COL_SEP + key + COL_SEP + value;
            string recMsg = sendMsg(getXpushPacket(pushMsg));

            return recMsg;
        }
        catch (Exception e)
        {
            string strMessage = "[Unexpected exception]" + e.Message;
            comActLog.ExceptionLogWrite(strMessage);

            return null;
        }
    }

    private string sendBye()
    {
        try
        {
            string byeMsg = ACTIONTYPE_BYEC;
            string recMsg = sendMsg(getXpushPacket(byeMsg));

            return recMsg;
        }
        catch (Exception e)
        {
            string strMessage = "[Unexpected exception]" + e.Message;
            comActLog.ExceptionLogWrite(strMessage);

            return null;
        }
    }

    private string sendKeep()
    {
        try
        {
            string keepMsg = String.Empty;
            string recMsg = sendMsg(getXpushPacket(keepMsg));

            return recMsg;
        }
        catch (Exception e)
        {
            string strMessage = "[Unexpected exception]" + e.Message;
            comActLog.ExceptionLogWrite(strMessage);

            return null;
        }
    }

    private string sendMsg(byte[] sendBytes)
    {
        try
        {
            // 메시지 전송
            int iSent = providerSocket.Send(sendBytes);

            // 응답
            byte[] recBytes = new byte[1024];
            int iRec = providerSocket.Receive(recBytes);

            string recMsg = utf8ENC.GetString(recBytes, 0, iRec);
            
            return recMsg;
        }
        catch (SocketException se)
        {
            string strMessage = "[SocketException]" + se.Message;
            comActLog.ExceptionLogWrite(strMessage);

            return null;

        }
        catch (Exception e)
        {
            string strMessage = "[Unexpected exception]" + e.Message;
            comActLog.ExceptionLogWrite(strMessage);

            return null;
        }
    }

    private byte[] getXpushPacket(string msg)
    {
        byte[] msgBytes = utf8ENC.GetBytes(msg);

        //메시지 사이즈 ex)000010
        int msgSize = msgBytes.Length + 1; //종료문자 + 1
        byte[] sizeBytes = utf8ENC.GetBytes((msgSize).ToString().PadLeft(6, '0'));

        byte[] bytes = new byte[sizeBytes.Length + msgBytes.Length + 1];

        Array.Copy(sizeBytes, bytes, sizeBytes.Length);
        Array.Copy(msgBytes, 0, bytes, sizeBytes.Length, msgBytes.Length);
        bytes[bytes.Length - 1] = Convert.ToByte(TERM_CODE);

        //로그
        comActLog.LogWrite(BitConverter.ToString(bytes));

        return bytes;
    }

    public void connect()
    {
        connectSocket();

        string rcv = sendAuth();
        comActLog.LogWrite("[XPUSHPROVIDER:RECEIVEAUTH]" + rcv);
    }

    public void push(string type, string key, string value)
    {
        string rcv = sendPush(type, key, value);
        comActLog.LogWrite("[XPUSHPROVIDER:RECEIVEPUSH]" + rcv);
    }

    public void shutdown()
    {
        string rcv = sendBye();
        comActLog.LogWrite("[XPUSHPROVIDER:RECEIVEBYEC]" + rcv);

        providerSocket.Shutdown(SocketShutdown.Both);
        providerSocket.Close();
    }
}
