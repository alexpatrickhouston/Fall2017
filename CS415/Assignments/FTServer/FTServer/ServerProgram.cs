using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using PRSMessageLibrary;

namespace FTServer
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            ushort listeningPort = 40001;
           string serviceName = "FT Server";
            string prsIP = "127.0.0.1";         // TODO: get prsIP from the cmdline
            ushort prsPort = 30000;
            // TODO: process cmd line
            // -prs <PRS IP address>:<PRS port>
            // -p listening port
            if (args != null)//if we have command line options
            {
                string[] command = args;
                if ((command.Length) % 2 != 0)//if each command doesnt have a 
                {
                    throw (new Exception("Invalid Command Line Args"));
                }
                for (int i = 0; i < command.Length; i = i + 2)
                {
                    switch (command[i].ToLower())
                    {
                        case "-prs"://prs command gotten
                            string[] ipportstring = command[i + 1].Split(':');
                            prsIP = ipportstring[0];
                            prsPort = Convert.ToUInt16( ipportstring[1]);
                            break;
                        case "-p"://port to listen on
                            listeningPort = Convert.ToUInt16(command[i + 1]);
                            break;
                        default:
                            break;
                    }
                }
            }

            // get the listening port from the PRS for the "FT Server" service
           // TODO: get prsPort from the cmdline
            PRSServiceClient.prsAddress = IPAddress.Parse(prsIP);
            PRSServiceClient.prsPort = prsPort;
            PRSServiceClient prs = new PRSServiceClient(serviceName);
            listeningPort = prs.RequestPort();

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
            
            // close the listening port that I received from the PRS
            prs.ClosePort();
        }

        class ClientThread
        {
            static ulong nextFile = 0;

            private Thread theThread;
            private Socket clientSocket;

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
                NetworkStream NetworkSocketStream = new NetworkStream(clientSocket);
                StreamReader SocketReader = new StreamReader(NetworkSocketStream);
                StreamWriter SocketWriter = new StreamWriter(NetworkSocketStream);
                while (!done && clientSocket.Connected)
                {
                    string cmd = SocketReader.ReadLine();
                    if(cmd == null)
                    {
                        done = true;
                        break;
                    }
                    Console.WriteLine("received cmd" + cmd);

                    // parse the client's request to find the cmd

                    switch (cmd)
                    {
                        case "get":
                            {
                                Console.WriteLine("Received GET cmd from client");
                                string directoryName = SocketReader.ReadLine();
                                Console.WriteLine("Getting files for directory " + directoryName);
                                // open the named directory
                                DirectoryInfo di = new DirectoryInfo(directoryName);

                                // send each file to the client
                                foreach (FileInfo fi in di.EnumerateFiles())
                                {
                                    Console.WriteLine("Found file " + fi.Name + " in directory");
                                    if (fi.Extension == ".txt")
                                    {

                                        
                                        Console.WriteLine("Found TXT file: " + fi.Name);

                                        // get a new port that we can use to send the file to the client
                                        string serviceName = "FT Server - File " + (nextFile++).ToString();
                                        PRSServiceClient prs = new PRSServiceClient(serviceName);
                                        ushort filePort = prs.RequestPort();

                                        SocketWriter.WriteLine(fi.Name);
                                        SocketWriter.WriteLine(fi.Length.ToString());
                                        SocketWriter.Flush();
                                        FileStream fileStream = fi.OpenRead();
                                        StreamReader fileReader = new StreamReader(fileStream);
                                        string fileContents = fileReader.ReadToEnd();
                                        SocketWriter.Write(fileContents);
                                        SocketWriter.Flush();
                                        fileReader.Close();
                                        fileStream.Close();
                                    }
                                }
                                SocketWriter.WriteLine("done");
                                SocketWriter.Flush();
                            }
                            break;

                        case "exit":
                            Console.WriteLine("Received EXIT cmd from client");
                            done = true;
                            break;
                    }
                }

                // disconnect from client and close the socket
                Console.WriteLine("Disconnecting from client");
                clientSocket.Disconnect(false);
                NetworkSocketStream.Close();
                SocketWriter.Close();
                SocketReader.Close();
                clientSocket.Close();
                Console.WriteLine("Disconnected from client");
            }

            private static void ClientThreadFunc(object data)
            {
                Console.WriteLine("Client thread started");
                ClientThread ct = data as ClientThread;
                ct.Run();
            }
        }
    }


}
