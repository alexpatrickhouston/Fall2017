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
        //Default values
        //PRS options
        static string PRS_ADDRESS = "127.0.0.1";
        static ushort PRS_PORT = 30000;
        //service options
        static string SERVICE_NAME = "SD Client";
        static string SERVER_ADDRESS = "127.0.0.1";
        //Cmd line tags
        static bool OPEN_SESSION = false;
        static bool CLOSE_SESSION = false;
        static bool RESUME_SESSION = false;
        static bool GET = false;
        static bool POST = false;
        static ulong SESSION_ID = 0;
        static string DOCUMENT_NAME = null;


        static void Main(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-prs")// -prs prsip:prsport
                    {
                        if (i++ < args.Length)
                        {
                            string[] parts = args[i].Split(':');
                            if (parts.Length != 2)
                                throw new Exception("Invalid PRSIP:PRSAddress format");
                            PRS_ADDRESS = parts[1];
                            PRS_PORT = System.Convert.ToUInt16(parts[0]);
                        }
                        else
                        {
                            throw new Exception("No value for -prs option");
                        }
                    }
                    else if (args[i] == "-get")//-get documentname
                    {
                        if (i++ < args.Length)
                        {
                            DOCUMENT_NAME = args[i];
                            GET = true;
                        }
                        else
                        {
                            throw new Exception("No value for -get option");
                        }
                    }
                    else if (args[i] == "-post")//-post documentname
                    {
                        if (i++ < args.Length)
                        {
                            DOCUMENT_NAME = args[i];
                            POST = true;
                        }
                        else
                        {
                            throw new Exception("No value for -post option");
                        }
                    }
                    else if (args[i] == "-o")//-o
                    {
                        OPEN_SESSION = true;
                    }
                    else if (args[i] == "-r")//-r sessionid
                    {
                        if (i++ < args.Length)
                        {
                            SESSION_ID = System.Convert.ToUInt64(args[i]);
                            RESUME_SESSION = true;
                        }
                        else
                        {
                            throw new Exception("No value for -r option");
                        }

                    }
                    else if (args[i] == "-c")//-c sessionid | -r sessionid -c | -o sessionid -c
                    {
                        if (i++ < args.Length)
                        {
                            SESSION_ID = System.Convert.ToUInt64(args[i]);
                            CLOSE_SESSION = true;
                        }
                        else
                        {
                            throw new Exception("No value for -o option");
                        }
                    }
                    else if (args[i] == "-s")//-s serveraddress
                    {
                        if (i++ < args.Length)
                        {
                            SERVER_ADDRESS = args[i];
                        }
                        else
                        {
                            throw new Exception("No value for -s option");
                        }
                    }
                    else
                        throw new Exception("Invalid cmdline parameter");

                }
            }catch(Exception ex)
            {
                Console.WriteLine("Error processing command line:" + ex);
                return;
            }
            Console.WriteLine("PRS ADDRESS:"+PRS_ADDRESS);
            Console.WriteLine("PRS PORT:"+PRS_PORT);
            Console.WriteLine("SERVER ADDRESS"+SERVER_ADDRESS);
            Console.WriteLine("OPEN SESSION:"+ OPEN_SESSION.ToString());
            Console.WriteLine("CLOSE SESSION:" +CLOSE_SESSION.ToString());
            Console.WriteLine("RESUME SESSION:"+ RESUME_SESSION.ToString());
            Console.WriteLine("RESUME SESSION ID:"+SESSION_ID.ToString());
            Console.WriteLine("GET:"+GET.ToString());
            Console.WriteLine("POST:"+ POST.ToString());
            Console.WriteLine("DOCUMENTNAME"+ DOCUMENT_NAME);
            Console.WriteLine();



            // Get port from the PRS
            PRSClient prs = new PRSClient(IPAddress.Parse(PRS_ADDRESS), PRS_PORT, SERVICE_NAME);
            ushort serverPort = prs.LookupPort();
            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + SERVER_ADDRESS + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(SERVER_ADDRESS), serverPort);
            Console.WriteLine("Connected to server");
            //establish network stream
            NetworkStream SocketNetworkStream = new NetworkStream(sock);
            StreamReader socketReader = new StreamReader(SocketNetworkStream);
            StreamWriter socketWriter = new StreamWriter(SocketNetworkStream);



            
            //order of commands is o or r then get or post then close
            if(OPEN_SESSION)
            {
                SESSION_ID =OpenSession(socketReader, socketWriter);
            }
            else if(RESUME_SESSION)
            {
                ResumeSession(socketReader, socketWriter);
            }

            //get/post block
            if(GET)
            {
                Get(socketReader, socketWriter);
            }
            else if(POST)
            {
                Post(socketReader, socketWriter);
            }
            //close block
            if(CLOSE_SESSION)
            {
                CloseSession(socketReader, socketWriter);
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

            // disconnect from the server and close socket
            Console.WriteLine("Disconnecting from server");
            sock.Disconnect(false);
            socketReader.Close();
            socketWriter.Close();
            SocketNetworkStream.Close();
            sock.Close();
            Console.WriteLine("Disconnected from server");
        }
        static ulong OpenSession(StreamReader socketReader, StreamWriter socketWriter)
        {
            ulong SessionID = 0;
            Console.WriteLine("Sending open to server");
            socketWriter.WriteLine("open");
            socketWriter.Flush();
            //Recieve accept from the server
            string responsestring = socketReader.ReadLine();
            Console.WriteLine("Recieved" + responsestring);
            if (responsestring == "accepted")
            {
                //Console.WriteLine("Recieved an accepted from the server");
                responsestring = socketReader.ReadLine();
                SessionID = Convert.ToUInt64(responsestring);
                Console.WriteLine("Session ID Recieved:" + SessionID.ToString());
            }
            else
            {
                Console.WriteLine("Recieved invalid response " + responsestring);
            }
            return SessionID;
        }
        static ulong CloseSession(StreamReader socketReader, StreamWriter socketWriter)
        {
            Console.WriteLine("Sending Close to the server");

            //Close message  is close<\n><session id><\n>
            socketWriter.WriteLine("close");
            socketWriter.WriteLine(SESSION_ID);
            socketWriter.Flush();
            string responsestring = socketReader.ReadLine();
            if (responsestring == "Closed")// closed <\n >< session id ><\n >
            {
                Console.WriteLine("Recieved an Closed from the server");
                responsestring = socketReader.ReadLine();
                Console.WriteLine("Resumed session with id:" + responsestring);
            }
            else
                Console.WriteLine("Invalid Response recieved from the server");
            return SESSION_ID;
        }
        static ulong ResumeSession(StreamReader socketReader, StreamWriter socketWriter)
        {
           
            Console.WriteLine("Sending Resume to the server");

            //Resume Message is resume\nsessionid\n
            socketWriter.WriteLine("resume");
            socketWriter.WriteLine(SESSION_ID);
            socketWriter.Flush();
            string responsestring = socketReader.ReadLine();
            if (responsestring == "accepted")//accepted<\n><session id><\n>
            {
                Console.WriteLine("Recieved an accepted from the server");
                responsestring = socketReader.ReadLine();
                Console.WriteLine("Resumed session with id:" + responsestring);
            }
            else if (responsestring == "rejected")// rejected <\n >< reason string><\n >
            {
                Console.WriteLine("Recieved a rejected message from the server");
                responsestring = socketReader.ReadLine();
                Console.WriteLine("Reason for the rejection:" + responsestring);
            }
            else
                Console.WriteLine("Invalid Response recieved from the server");
            return SESSION_ID;
        }
        static void Get(StreamReader socketReader, StreamWriter socketWriter)
        {
            Console.WriteLine("Sending Get to the server");
            string doc = DOCUMENT_NAME;
            socketWriter.WriteLine("get");
            socketWriter.WriteLine(doc);
            socketWriter.Flush();
            string responsestring = socketReader.ReadLine();
            if (responsestring == "Success")
            {
                Console.WriteLine("Recieved a success from the server");
                responsestring = socketReader.ReadLine();
                if (responsestring == doc)
                {
                    Console.WriteLine("Recieved correct document");
                    responsestring = socketReader.ReadLine();
                    int length = System.Convert.ToInt32(responsestring);
                    Console.WriteLine("Recieved length:" + length);
                    char[] buffer = new char[length];
                    int result = socketReader.Read(buffer, 0, length);
                    if (result == length)
                    {
                        string documentcontent = new string(buffer);
                        Console.WriteLine("Recieved " + result + " bytes of data for file ");
                        Console.WriteLine("File Contents: " + documentcontent);
                    }
                    else
                        Console.WriteLine("Error expected " + length + " bytes but recieved " + result);
                }
                else
                    Console.WriteLine("Recieved incorrect document, recieved " + doc);
            }
        }
        static void Post(StreamReader socketReader, StreamWriter socketWriter)
        {
            string documentcontents = "";
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                documentcontents += line + "\n";
            }
            string docname = DOCUMENT_NAME;
            Console.WriteLine("Sending post for document " + docname + " to Server");
            socketWriter.WriteLine("post");
            socketWriter.WriteLine(docname);
            socketWriter.WriteLine(documentcontents.Length.ToString());
            socketWriter.Write(documentcontents);
            socketWriter.Flush();
            string responseString = socketReader.ReadLine();
            if (responseString == "success")
            {
                Console.WriteLine("Success!");

            }
            else if(responseString == "error")
            {
                responseString = socketReader.ReadLine();
                Console.WriteLine("Error recieved from Server:" + responseString);
            }
            else
                Console.WriteLine("Invalid response recieved from the Server");
        }
    }
}


