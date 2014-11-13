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

/// <summary>
/// XPushProvider 클래스
/// 유종원
/// 14. 11. 13
/// </summary>
public class XPushProvider
{
    Encoding exASCIIENC;
    IPEndPoint serverAddress;
    Socket providerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    ActLog comActLog = new ActLog();

    private static Char COL_SEP = (char)0x11; // 데이터간 구분자
    private static Char TERM_CODE = (char)0xff; // 패킷 종료 문자

    private string id = "id";
    private string pw = "pw";


    

    public XPushProvider()
    {
        //
        // TODO: 여기에 생성자 논리를 추가합니다.
        //

        // 확장아스키코드사용
        exASCIIENC = Encoding.GetEncoding(28591);

        string ipAddress = ConfigurationSettings.AppSettings["XPushProviderIP"].ToString();
        int portNo = Convert.ToInt16(ConfigurationSettings.AppSettings["XPushProviderPort"].ToString());

        serverAddress = new IPEndPoint(IPAddress.Parse(ipAddress), portNo);
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

    private void connectSocket()
    {
        try
        {
            // 소켓 연결
            providerSocket.Connect(serverAddress);
        }
        catch (SocketException se)
        {
            string strMessage = "[SocketException]" + se.Message;
            comActLog.ExceptionLogWrite(strMessage);

        }
        catch (Exception e)
        {
            string strMessage = "[Unexpected exception]" + e.Message;
            comActLog.ExceptionLogWrite(strMessage);
        }
    }

    private string sendMsg(string sendMsg)
    {
        try
        {
            byte[] sendBytes = exASCIIENC.GetBytes(sendMsg);

            //테스트용 AUTHidpw
            //30-30-30-30-31-30-41-55-54-48-69-64-11-70-77-FF
            string hex = BitConverter.ToString(sendBytes);
            comActLog.LogWrite(hex);
            //---------
            
            // 메시지 전송
            int iSent = providerSocket.Send(sendBytes);
            
            // 응답
            byte[] recBytes = new byte[1024];
            int iRec = providerSocket.Receive(recBytes);
            
            string recMsg = exASCIIENC.GetString(recBytes, 0, iRec);
            comActLog.LogWrite("[XPUSHPROVIDER:RECEIVE]" + recMsg);

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

    private string sendAuth()
    {
        string authMsg = getAuthMsg();
        comActLog.LogWrite("[XPUSHPROVIDER:SEND]" + authMsg);

        string recMsg = sendMsg(authMsg);

        return recMsg;
    }

    private void sendPush(string type, string key, string value)
    {
        string recMsg = sendMsg(getPushMsg(type, key, value));
        comActLog.LogWrite("[XPUSHPROVIDER:SEND]" + getPushMsg(type, key, value));
    }

    private void sendBye()
    {
        string byeMsg = "BYEC" + TERM_CODE;
        string size = ((byeMsg).Length).ToString().PadLeft(6, '0');

        sendMsg(byeMsg);
    }

    public void connect()
    {
        connectSocket();
        string ret = sendAuth();

        if (ret.Substring(6, 2).Equals("OK"))
        {
            comActLog.LogWrite("[XPUSHPROVIDER]" + "접속성공");
        }
        else
        {
            comActLog.LogWrite("[XPUSHPROVIDER]" + "접속실패");
        }
    }

    public void push(string type, string key, string value)
    {
        sendPush(type, key, value);
    }

    public void closeSocket()
    {
        providerSocket.Shutdown(SocketShutdown.Both);
        providerSocket.Close();
    }
}
