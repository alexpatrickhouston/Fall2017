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
            // create the socket for sending messages to the server
            Socket clientSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Console.WriteLine("Socket created");

            // construct the server's address and port


            try
            {
                TestCase1(clientSocket);
                TestCase2(clientSocket);
                StopServer(clientSocket);

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

        private static void StopServer(Socket clientsocket)
        {
            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(PRSCommunicator.DEFAULT_IP), PRSCommunicator.DEFAULT_PORT);
            PRSCommunicator.SendMessage(clientsocket, endPt, PRSMessage.MakeSTOP());
        }

        private static void TestCase1(Socket clientsocket)
        {
            //TestCase 1: 
            //FTP server requests port from prs
            //recieves port number
            //ftp server sends close port
            //recieves success
            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(PRSCommunicator.DEFAULT_IP), PRSCommunicator.DEFAULT_PORT);
            string FTPServer = "FTP";
            ushort allocatedport = 0;
            //Send request port
            PRSCommunicator.SendMessage(clientsocket, endPt, PRSMessage.MakeREQUEST_PORT(FTPServer));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            PRSMessage statusMsg = PRSCommunicator.ReceiveMessage(clientsocket, ref remoteEP);
           //PRSCommunicator.PrintMessage(statusMsg);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase1 failed on Request Port");
                allocatedport = statusMsg.port;
                Console.WriteLine("Allocated port of " + allocatedport.ToString());
            PRSCommunicator.SendMessage(clientsocket, endPt, PRSMessage.MakeKEEP_ALIVE(FTPServer, allocatedport));

            statusMsg = PRSCommunicator.ReceiveMessage(clientsocket, ref remoteEP);
            //PRSCommunicator.PrintMessage(statusMsg);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase1 failed on Keep Alive");
            PRSCommunicator.SendMessage(clientsocket, endPt, PRSMessage.MakeCLOSE_PORT(FTPServer, allocatedport));
            statusMsg = PRSCommunicator.ReceiveMessage(clientsocket, ref remoteEP);
            //PRSCommunicator.PrintMessage(statusMsg);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase1 failed on ClosePort");
        }
        private static void TestCase2(Socket clientsocket)//Lookup port dead
        {
            //TestCase 2:
            //FTP server reserves a port
            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(PRSCommunicator.DEFAULT_IP), PRSCommunicator.DEFAULT_PORT);
            string FTPServer = "FTP";
            ushort allocatedport = 0;
            ushort requestedport = 0;
            //Send request port
            PRSCommunicator.SendMessage(clientsocket, endPt, PRSMessage.MakeREQUEST_PORT(FTPServer));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            PRSMessage statusMsg = PRSCommunicator.ReceiveMessage(clientsocket, ref remoteEP);
            //PRSCommunicator.PrintMessage(statusMsg);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase2 failed on Request Port");
            allocatedport = statusMsg.port;
            Console.WriteLine("Allocated port of " + allocatedport.ToString());
            //FTP Client asks for the port that the client is on
            PRSCommunicator.SendMessage(clientsocket, endPt, PRSMessage.MakeLOOKUP_PORT("FTP"));
            //FTP Client "attempts" to connect to server
            statusMsg = PRSCommunicator.ReceiveMessage(clientsocket, ref remoteEP);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase2 failed on LookupPort");
            requestedport = statusMsg.port;
            Console.WriteLine("Service is on port: " + allocatedport.ToString());
            //FTP Client fails to connect 
            //FTP client sends port dead to PRS
            PRSCommunicator.SendMessage(clientsocket, endPt, PRSMessage.MakePORT_DEAD(requestedport));
            statusMsg = PRSCommunicator.ReceiveMessage(clientsocket, ref remoteEP);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase2 failed on Deadport");

        }

    }
}
