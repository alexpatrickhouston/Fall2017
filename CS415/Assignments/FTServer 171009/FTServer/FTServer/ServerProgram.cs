using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTServer
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            // TODO: get the listening port from the PRS for the "FT Server" service
            string serviceName = "FT Server";
            string prsIP = "127.0.0.1";
            ushort prsPort = 30000;
            PRSServiceClient.prsPort = prsPort;
            PRSServiceClient.prsAddress = IPAddress.Parse(prsIP);
            PRSServiceClient prs = new PRSServiceClient(serviceName);
            ushort listeningPort = prs.RequestPort();

            // create the TCP listening socket
            Socket listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
            listeningSocket.Listen(42);     // 42 is the number of clients that can be waiting for us to accept their connection
            Console.WriteLine("Listening for clients on port " + listeningPort.ToString());

            bool done = false;
            while (!done)
            {
                // wait for a client to connect
                Console.WriteLine("Ready to accept new client");
                Socket clientSocket = listeningSocket.Accept();
                Console.WriteLine("Accepted connection from client");
                // TODO: worry about keep alives to the PRS for our FT Server port number

                // create a thread for this client, and then return to listening for more clients
                Console.WriteLine("Launch new thread for connected client");
                ClientThread clientThread = new ClientThread(clientSocket);
                clientThread.Start();
            }

            // close down the listening socket
            Console.WriteLine("Closing listening socket");
            listeningSocket.Close();
        }

        class ClientThread
        {
            private Thread theThread;
            private Socket clientSocket;
            static ulong nextfile = 0;
            public ClientThread(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                theThread = new Thread(new ParameterizedThreadStart(ClientThreadFunc));
            }

            public void Start()
            {
                // Start the encapsulated thread
                // pass the instance of this class "ClientThread" to the thread so it can operate upon it
                theThread.Start(this);
            }

            private void Run()
            {
                bool done = false;
                while (!done)
                {
                    // get up to 256 bytes of data from the client
                    byte[] buffer = new byte[256];
                    int length = clientSocket.Receive(buffer);
                    if (length >= 0)//if client is disconnected
                        break;
                    string cmdstring = new string(ASCIIEncoding.UTF8.GetChars(buffer));
                    cmdstring = cmdstring.TrimEnd('\0');
                    Console.WriteLine("received " + length.ToString() + " bytes from client: " + cmdstring);
                    //TODO:Actually part cmd
                    string cmd = cmdstring.Substring(0, cmdstring.IndexOf(' '));//gets the cmd 
                    switch (cmd)
                    {
                        case "get":

                            string directoryName = "foo";
                            DirectoryInfo di = new DirectoryInfo(directoryName);
                            Console.WriteLine("Getting files for directory" + directoryName);
                // disconnect from client and close the socket
                Console.WriteLine("Disconnecting from client");
                            clientSocket.Disconnect(false);
                            clientSocket.Close();
                            Console.WriteLine("Disconnected from client");
                            break;
                        case "exit":
                            Console.WriteLine("Recieved Exit command");
                            break;
                    }
                }
            }

            private static void ClientThreadFunc(object data)
            {
                Console.WriteLine("Client thread started");
                ClientThread ct = data as ClientThread;
                ct.Run();
            }
        }
    }
    class PRSServiceClient
    {
        public static IPAddress prsAddress;
        public static ushort prsPort;
        public PRSServiceClient(string serviceName)
        {

        }
        public ushort RequestPort()
        {
            return 40001;
        }
        public void ClosePort()
        {
           
        }
        private void KeepAlive()
        {

        }
    }
}
