using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApplication1
{
    class SynchronousSocketClient
    {
        public static Char COL_SEP = (char)0x11;
        public static Char TERM_CODE = (char)0xff;

        public static void StartClient()
        {

            


            // Data buffer for incoming data.
            byte[] bytes = new byte[1024];

            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // This example uses port 11000 on the local computer.

                //IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName())
                //IPAddress ipAddress = ipHostInfo.AddressList[0];

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 10082);

                // Create a TCP/IP  socket.
                Socket sender = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());


                    String authMsg = "AUTH" + "id" + COL_SEP + "pw" + TERM_CODE;
                    int len = (authMsg).Length;
                    String size = (len).ToString().PadLeft(6, '0');
                    String toSend = size + authMsg;


                    Encoding enc = Encoding.GetEncoding(28591);
                    


                    // Encode the data string into a byte array.
                    byte[] msg = enc.GetBytes(toSend);

                    string hex = BitConverter.ToString(msg);
                    Console.WriteLine(hex);

                    /*
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(msg);


                    hex = BitConverter.ToString(msg);
                    Console.WriteLine(hex);
                    */


                    // Send the data through the socket.
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}",
                        enc.GetString(bytes, 0, bytesRec));

                    






                    // Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }





        public static int Main(String[] args)
        {
            StartClient();
            return 0;
        }
    }
}
