using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace PRSMessageLibrary
{
    public class PRSCommunicator
    {
        //Standard message Procedures and Defaults
        public const int DEFAULT_PORT = 30000;
        public const int DEFAULT_STARTING = 40000;
        public const int DEFAULT_ENDING = 40099;
        public const int DEFAULT_TIMEOUT = 300;
        public const string DEFAULT_IP = "127.0.0.1";
        public static void SendMessage(Socket sock, IPEndPoint endPt, PRSMessage msg)
        {
            PrintMessage(msg);
            Console.WriteLine("Sending message to server...");
            byte[] buffer = msg.Serialize();
            int result = sock.SendTo(buffer, endPt);
            //Console.WriteLine("Sent " + result.ToString() + " bytes: " + new string(ASCIIEncoding.UTF8.GetChars(buffer)));
        }

        public static PRSMessage ReceiveMessage(Socket sock, ref IPEndPoint remoteIPEP)
        {
            Console.WriteLine("Waiting for message from client...");
            byte[] buffer = new byte[PRSMessage.SIZE];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            int result = sock.ReceiveFrom(buffer, ref remoteEP);
            remoteIPEP = (IPEndPoint)remoteEP;
            //Console.WriteLine("Received " + result.ToString() + " bytes: " + new string(ASCIIEncoding.UTF8.GetChars(buffer)));
            // deserialize and handle the message
            PRSMessage msg = PRSMessage.Deserialize(buffer);

            return msg;
        }
        public static void PrintMessage(PRSMessage msg)
        {
            Console.WriteLine("Message type:" + msg.msgType);
            Console.WriteLine("Status:" + msg.status);
            Console.WriteLine("Port" + msg.port);
            Console.WriteLine("Service:" + msg.serviceName+"\n");
        }
    }
}
