using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;

namespace WebApplication1
{
    public partial class _Default : System.Web.UI.Page
    {
        public static Char COL_SEP = (char)0x11;
        public static Char TERM_CODE = (char)0xff;

        //확장아스키코드사용
        Encoding enc = Encoding.GetEncoding(28591);

        IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 10082);
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        protected void Page_Load(object sender, EventArgs e)
        {
            Label1.Text = "Start";
            
            msg("Client Started");

            //clientSocket.Connect("192.168.0.1", 10082);
            //clientSocket.Connect(serverAddress);
            Label1.Text = "Socket Connected";
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            String authMsg = "AUTH" + "id" + COL_SEP + "pw" + TERM_CODE;
            int len = (authMsg).Length;
            String size = (len).ToString().PadLeft(6, '0');
            String toSend = size + authMsg;

            msg(toSend);
            try
            {

                clientSocket.Connect(serverAddress);

                byte[] sBytes = enc.GetBytes(toSend);

                string hex = BitConverter.ToString(sBytes);
                msg(hex);


                int bytesSent = clientSocket.Send(sBytes);


                byte[] rBytes = new byte[1024];
                int bytesRec = clientSocket.Receive(rBytes);

                String rMsg = enc.GetString(rBytes, 0, bytesRec);

                msg(rMsg);

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (ArgumentNullException ane)
            {
                msg("ArgumentNullException : " + ane.ToString());
            }
            catch (SocketException se)
            {
                msg("SocketException : " +  se.ToString());
            }
            catch (Exception e2)
            {
                msg("Unexpected exception : " + e2.ToString());
            }
                      
       
    }


        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        

        public void msg(string mesg)
        {
            TextBox1.Text = TextBox1.Text + Environment.NewLine + " >> " + mesg;
        }

        protected void Button2_Click(object sender, EventArgs e)
        {

            
            

            String authMsg = "PUSH" + "CPDT" + COL_SEP + "1001" + COL_SEP + "99399" + TERM_CODE;
            int len = (authMsg).Length;
            String size = (len).ToString().PadLeft(6, '0');
            String toSend = size + authMsg;

            msg(toSend);
            try
            {

                clientSocket.Connect(serverAddress);

                byte[] sBytes = enc.GetBytes(toSend);

                string hex = BitConverter.ToString(sBytes);
                msg(hex);


                int bytesSent = clientSocket.Send(sBytes);


                byte[] rBytes = new byte[1024];
                int bytesRec = clientSocket.Receive(rBytes);

                String rMsg = enc.GetString(rBytes, 0, bytesRec);

                msg(rMsg);

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (ArgumentNullException ane)
            {
                msg("ArgumentNullException : " + ane.ToString());
            }
            catch (SocketException se)
            {
                msg("SocketException : " + se.ToString());
            }
            catch (Exception e2)
            {
                msg("Unexpected exception : " + e2.ToString());
            }

        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        } 
    }
}
