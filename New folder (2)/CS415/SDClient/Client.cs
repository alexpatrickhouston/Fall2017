using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using PRSMessageLibrary;

namespace SDClient
{
    class ClientProgram
    {
        static void Main(string[] args)
        {
            // TODO: get the server port from the PRS for the "FT Server" service

            // TODO: get the server's IP from the command line
            string serverIP = "127.0.0.1";
            string prsIP = "127.0.0.1";
            ushort prsPort = 30000;
            // TODO: get the directory name from the command line
            string directoryName = "foo";
            if (args != null)
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
                            prsPort = Convert.ToUInt16(ipportstring[1]);
                            break;
                        case "-s"://port to listen on
                            serverIP = command[i + 1];
                            break;
                        case "-o":
                            break;
                        case "-r":
                            break;
                        case "-c":
                            break;
                        case "-get":
                            break;
                        case "-post":
                            break;

                        case "-d"://port to listen on
                            directoryName = command[i + 1];
                            break;
                        default:
                            break;
                    }
                }
            }

            // Get port from the PRS
            PRSServiceClient prs = new PRSServiceClient("SDServer");
            ushort serverPort = prs.LookupPort();
            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(serverIP), serverPort);
            Console.WriteLine("Connected to server");
            //establish network stream
            NetworkStream SocketNetworkStream = new NetworkStream(sock);
            StreamReader socketReader = new StreamReader(SocketNetworkStream);
            StreamWriter socketWriter = new StreamWriter(SocketNetworkStream);


            //Send an open session with the server
            int SessionID = 0;
            socketWriter.WriteLine("open");
            socketWriter.Flush();
            //Recieve accept from the server
            string responsestring = socketReader.ReadLine();
            if(responsestring == "Accepted")
            {
                Console.WriteLine("Recieved an accepted from the server");
                responsestring = socketReader.ReadLine();
                SessionID = Convert.ToInt32(responsestring);
            }
            Console.WriteLine("Sending Get to the server");
            string doc = "foo";
            socketWriter.WriteLine("get");
            socketWriter.WriteLine(doc);
            socketWriter.Flush();
            responsestring = socketReader.ReadLine();
            if (responsestring == "Success")
            {
                Console.WriteLine("Recieved a success from the server");
                responsestring = socketReader.ReadLine();
                if (responsestring == doc)
                    Console.WriteLine("Recieved correct document");
                else
                    Console.WriteLine("Recieved incorrect document, recieved " +doc);
            }
            //socketWriter.WriteLine("get");
            //socketWriter.WriteLine(directoryName);
            //socketWriter.Flush();
            //Console.WriteLine("Sent get " + directoryName);

            //bool done = false;
            //while (!done)
            //{
            //    string cmdstring = socketReader.ReadLine();
            //    if (cmdstring == "done")
            //        done = true;
            //    else
            //    {
            //        string filename = cmdstring;
            //        string lengthstring = socketReader.ReadLine();
            //        int filelength = System.Convert.ToInt32(lengthstring);
            //        char[] buffer = new char[filelength];
            //        socketReader.Read(buffer, 0, filelength);
            //        string filecontents = new string(buffer);
            //        File.WriteAllText(Path.Combine(directoryName, filename), filecontents);
            //    }

            //}

            //// disconnect from the server and close socket
            //Console.WriteLine("Disconnecting from server");
            //sock.Disconnect(false);
            //socketReader.Close();
            //socketWriter.Close();
            //SocketNetworkStream.Close();
            //sock.Close();
            //Console.WriteLine("Disconnected from server");
        }
    }
}


