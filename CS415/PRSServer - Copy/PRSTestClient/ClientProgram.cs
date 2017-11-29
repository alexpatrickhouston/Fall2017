using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using PRSProtocolLibrary;

namespace PRSTestClient
{
    class ClientProgram
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
                string serviceName = "foo";
                ushort allocatedPort = 0;

                // send REQUEST_PORT
                PRSCommunicator.SendMessage(clientSocket, endPt, PRSMessage.CreateREQUEST_PORT(serviceName));

                // check status
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                PRSMessage statusMsg = PRSCommunicator.ReceiveMessage(clientSocket, ref remoteEP);
                if (statusMsg.status == PRSMessage.Status.SUCCESS)
                {
                    allocatedPort = statusMsg.port;
                    Console.WriteLine("Allocated port of " + allocatedPort.ToString());
                }
                else if (statusMsg.status == PRSMessage.Status.SERVICE_IN_USE)
                {
                    Console.WriteLine("service in use!");
                }
                else if (statusMsg.status == PRSMessage.Status.ALL_PORTS_BUSY)
                {
                    Console.WriteLine("all ports busy");
                }

                // send KEEP_ALIVE
                PRSCommunicator.SendMessage(clientSocket, endPt, PRSMessage.CreateKEEP_ALIVE(serviceName, allocatedPort));

                // check status
                statusMsg = PRSCommunicator.ReceiveMessage(clientSocket, ref remoteEP);
                if (statusMsg.status == PRSMessage.Status.SUCCESS)
                {
                    Console.WriteLine("success!! yay!");
                }
                
                // send CLOSE_PORT

                // check status

                // send STOP
                PRSCommunicator.SendMessage(clientSocket, endPt, PRSMessage.CreateSTOP());
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
