using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using PRSMessageLibrary;

namespace PRSTestClient
{
    class Client
    {

        static void Main(string[] args)
        {
            string ADDRESS = "127.0.0.1";
            int PORT = 30000;

            // create the socket for sending messages to the server
            Socket clientSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Console.WriteLine("Socket created");

            // construct the server's address and port
            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(ADDRESS), PORT);

            try
            {
                // send a message to the server
                Console.WriteLine("Sending message to server...");
                PRSMessage msg = new PRSMessage();
                msg.msgType = PRSMessage.MsgType.STOP;
                byte[] buffer = msg.Serialize();
                int result = clientSocket.SendTo(buffer, endPt);
                Console.WriteLine("Sent " + result.ToString() + " bytes: " + new string(ASCIIEncoding.UTF8.GetChars(buffer)));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception when receiving..." + ex.Message);
            }

            // close the socket and quit
            Console.WriteLine("Closing down");
            clientSocket.Close();
            Console.WriteLine("Closed!");

            Console.ReadKey();
        }
    }
}
