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
/// XPushProvider
/// 
/// </summary>
public class XPushProvider
{
    // 확장아스키코드사용
    Encoding exASCIIENC = Encoding.GetEncoding(28591);

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
    }

    public XPushProvider(string id, string pw)
    {
        this.id = id;
        this.pw = pw;
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
            //comActLog.LogWrite("[XPUSHPROVIDER:RECEIVE]" + recMsg);

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
        comActLog.LogWrite("[XPUSHPROVIDER:SENDAUTH]" + authMsg);

        string recMsg = sendMsg(authMsg);

        return recMsg;
    }

    private string sendPush(string type, string key, string value)
    {
        string pushMsg = getPushMsg(type, key, value);
        comActLog.LogWrite("[XPUSHPROVIDER:SENDPUSH]" + pushMsg);

        string recMsg = sendMsg(getPushMsg(type, key, value));
        
        return recMsg;
    }

    private string sendBye()
    {
        string byeMsg = "BYEC" + TERM_CODE;
        string size = ((byeMsg).Length).ToString().PadLeft(6, '0');
        byeMsg = size + byeMsg;

        comActLog.LogWrite("[XPUSHPROVIDER:SENDBYEC]" + byeMsg);

        string recMsg = sendMsg(byeMsg);

        return recMsg;
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
