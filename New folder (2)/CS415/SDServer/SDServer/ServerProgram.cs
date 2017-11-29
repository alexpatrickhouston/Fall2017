using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using PRSMessageLibrary;

namespace SDServer
{
    class ServerProgram
    {
        //Default Values
        static string PRS_ADDRESS = "127.0.0.1";
        static ushort PRS_PORT = 30000;
        //service options
        static string SERVICE_NAME = "SD SERVER";
        static int CLIENT_BACKLOG = 42;

        static void Main(string[] args)
        {
            // process cmd line
            // -prs <PRS IP address>:<PRS port>
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
                    else
                        throw new Exception("Invalid cmdline parameter");
                }
            }catch(Exception ex)
            {
                Console.WriteLine("Error processing command line:" + ex);
                return;
            }
            // create the session table
            SessionTable sessionTable = new SessionTable();
            Console.WriteLine("PRS ADDRESS:" + PRS_ADDRESS);
            Console.WriteLine("PRS PORT:" + PRS_PORT);
            // get the listening port from the PRS for the "SD Server" service
            PRSClient PRS = new PRSClient(IPAddress.Parse(PRS_ADDRESS), PRS_PORT, SERVICE_NAME);
            ushort listeningPort = PRS.RequestPort();

            // create the TCP listening socket
            Socket listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
            listeningSocket.Listen(CLIENT_BACKLOG);     // 42 is the number of clients that can be waiting for us to accept their connection
            Console.WriteLine("Listening for clients on port " + listeningPort.ToString());

            bool done = false;
            while (!done)
            {
                // wait for a client to connect
                Console.WriteLine("Ready to accept new client");
                Socket clientSocket = listeningSocket.Accept();
                Console.WriteLine("Accepted connection from client");
                
                // create a thread for this client, and then return to listening for more clients
                Console.WriteLine("Launch new thread for connected client");
                ClientThread clientThread = new ClientThread(clientSocket, sessionTable);
                clientThread.Start();
            }

            // close down the listening socket
            Console.WriteLine("Closing listening socket");
            listeningSocket.Close();

            // close the listening port that I received from the PRS
            PRS.ClosePort();
        }

        class ClientThread
        {
            private Thread theThread;

            private Socket clientSocket;
            private NetworkStream socketNetworkStream;
            private StreamReader socketReader;
            private StreamWriter socketWriter;

            private SessionTable sessionTable;
            private SDSession session;

            private State currentState;

            abstract class State
            {
                protected ClientThread client;


                public State(ClientThread client)
                {
                    this.client = client;
                }

                public abstract SDSession HandleOpenCmd();
                public abstract SDSession HandleResumeCmd();
                public void HandleCloseCmd()
                {
                    string requeststring = client.socketReader.ReadLine();//read the sessionid for the session to close
                    ulong closeSessionID = System.Convert.ToUInt64(requeststring);//convert it to a ulong
                    client.sessionTable.CloseSession(closeSessionID);//remove the key pair from the map
                    client.session = null;//set our referenced session to null

                    //close 
                    client.socketWriter.WriteLine("Closed");
                    client.socketWriter.WriteLine(closeSessionID);
                    client.socketWriter.Flush();
                }
                public abstract void HandleGetCmd();
                public abstract void HandlePostCmd();

                protected void SendError(string errorMsg)
                {
                    client.socketWriter.WriteLine("error");
                    client.socketWriter.WriteLine(errorMsg);
                    client.socketWriter.Flush();
                }
            }

            class ReadyForSessionCmd : State
            {
                public ReadyForSessionCmd(ClientThread client) :
                    base (client)
                {
                }

                override public SDSession HandleOpenCmd()
                {
                    // create a new session for the client
                    SDSession session = client.sessionTable.NewSession();
                    if (session != null)
                    {
                        Console.WriteLine("Creating new session with session ID" + session.ID);
                        // send Accepted(sessionId)
                        client.socketWriter.WriteLine("accepted");
                        client.socketWriter.WriteLine(session.ID.ToString());
                        client.socketWriter.Flush();

                        return session;
                    }

                    return null;
                }

                public override SDSession HandleResumeCmd()
                {
                    ulong sessionId =System.Convert.ToUInt64(client.socketReader.ReadLine());
                    SDSession session = client.sessionTable.Lookup(sessionId);//lookup the session ID
                    if(session == null)//The lookup was not valid
                    {
                        client.socketWriter.WriteLine("rejected");
                        client.socketWriter.WriteLine("Session does not exist");
                    }
                    return null;
                }



                public override void HandleGetCmd()
                {
                    client.socketReader.ReadLine();//eat the file name line 
                    SendError("No session open");//write message over to client
                }

                public override void HandlePostCmd()
                {
                    client.socketReader.ReadLine();//eat the file name line
                    SendError("No session open");//write message over to client
                }

            }

            class ReadyForDocumentCmd : State
            {
                public ReadyForDocumentCmd(ClientThread client) :
                    base (client)
                {
                }

                override public SDSession HandleOpenCmd()
                {
                    SendError("Session already open");
                    return null;
                }//Case already opened session

                public override SDSession HandleResumeCmd()
                {
                    client.socketReader.ReadLine();//eat the session id
                    SendError("Session already open");
                    return null;
                }//Case already opened session


                public override void HandleGetCmd()
                {
                    // read the document name from the client
                    string documentName = client.socketReader.ReadLine();
                    Console.WriteLine("Getting document " + documentName);

                    // lookup the document in the session
                    string documentContents = null;
                    if(documentName.Length >= 1 && documentName[0] == '/')
                        try
                        {
                            Console.WriteLine("Document is a file");
                            string filename = documentName.Substring(1);
                            documentContents = File.ReadAllText(filename);
                            Console.WriteLine("Found file " + filename);
                        }
                        catch (Exception ex)
                        {
                            SendError(ex.Message);
                        }
                    else
                    try
                    {
                            Console.WriteLine("Document is a session variable");
                            documentContents = client.session.GetValue(documentName);
                        Console.WriteLine("Found value " + documentName);
                        }
                        catch (Exception ex)
                        {
                            SendError(ex.Message);
                        }
                    // send the document name and length to the client
                    if (documentContents != null)
                    {
                        client.socketWriter.WriteLine("success");
                        client.socketWriter.WriteLine(documentName);
                        client.socketWriter.WriteLine(documentContents.Length.ToString());
                        client.socketWriter.Flush();

                        // send the document contents to the client
                        client.socketWriter.Write(documentContents);
                        client.socketWriter.Flush();

                        Console.WriteLine("Sent contents of " + documentName);
                    }
                    else
                    {
                        SendError("Document not found");
                    }
                }

                public override void HandlePostCmd()
                {
                    string documentName = client.socketReader.ReadLine();
                    Console.WriteLine("Posting Document " + documentName);

                    //read in document length
                    int doclength = System.Convert.ToInt32(client.socketReader.ReadLine());
                    Console.WriteLine("Document lengh is:" + doclength);
                    char[] buffer = new char[doclength];
                    int result = client.socketReader.Read(buffer, 0, doclength);
                    Console.WriteLine("Recieved " + result.ToString() + "bytes of the document");
                    if(result == doclength)
                    {
                        string documentContents = new string(buffer);
                        if (documentName.Length >= 1 && documentName[0] == '/')
                        {
                            try
                            {
                                Console.WriteLine("Document is a file");
                                string filename = documentName.Substring(1);
                                File.AppendAllText(filename, "\n" + documentContents);
                                Console.WriteLine("Appended contents to file" + filename);
                            }
                            catch (Exception ex)
                            {
                                SendError(ex.Message);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Document is a session variable");

                            client.session.PutValue(documentName, documentContents);
                            Console.WriteLine("Put document contents into session");
                        }
                        client.socketWriter.WriteLine("success");
                        client.socketWriter.Flush();
                        Console.WriteLine("Success Sent");
                    }
                    else
                    {
                        SendError("Recieved " + result.ToString() + " bytes of content but was expecting " + doclength + " bytes");
                    }

                }

            }

            public ClientThread(Socket clientSocket, SessionTable sessionTable)
            {
                socketNetworkStream = new NetworkStream(clientSocket);
                socketReader = new StreamReader(socketNetworkStream);
                socketWriter = new StreamWriter(socketNetworkStream);
                this.clientSocket = clientSocket;
                this.sessionTable = sessionTable;
                session = null;
                currentState = null;
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


                currentState = new ReadyForSessionCmd(this);

                bool done = false;
                while (!done && clientSocket.Connected)
                {
                    // read the next command from the client
                    string cmd = socketReader.ReadLine();
                    if (cmd == null)
                    {
                        // client disconnected
                        done = true;
                        break;
                    }
                    Console.WriteLine("Received cmd " + cmd);

                    switch (cmd)
                    {
                        case "close":
                            Console.WriteLine("Recieved Close command");
                            currentState.HandleCloseCmd();//handle the command
                            currentState = new ReadyForSessionCmd(this);
                            break;
                        case "open":
                            { 
                            Console.WriteLine("Received OPEN cmd from client");
                            session = currentState.HandleOpenCmd();
                            if (session == null)
                                Console.WriteLine("Unable to open new session");
                            else
                                currentState = new ReadyForDocumentCmd(this);
                            }
                            break;

                        case "resume":
                            {
                                Console.WriteLine("Received RESUME cmd from client");
                                // parse out the sessionId
                                session = currentState.HandleResumeCmd();
                                if (session != null)
                                {
                                    // successfully resumed session
                                    // change state
                                    currentState = new ReadyForDocumentCmd(this);
                                }
                                else
                                {
                                        Console.WriteLine("Unable to Resume session");
                                }
                            }
                            break;
                        case "post":
                            {
                                Console.WriteLine("Received POST cmd from client");
                                currentState.HandlePostCmd();
                                currentState = new ReadyForSessionCmd(this);
                            }
                            break;
                        case "get":
                            {
                                Console.WriteLine("Received GET cmd from client");
                                currentState.HandleGetCmd();
                                currentState = new ReadyForSessionCmd(this);
                            }
                            break;

                        case "exit":
                            Console.WriteLine("Received EXIT cmd from client");
                            done = true;
                            break;
                        default:
                            Console.WriteLine("Invalid Command Recieved from Client");
                            break;
                    }
                }

                // disconnect from client and close the socket, it's stream and reader/writer
                Console.WriteLine("Disconnecting from client");
                clientSocket.Disconnect(false);
                socketNetworkStream.Close();
                socketReader.Close();
                socketWriter.Close();
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

    class SDSession
    {
        private ulong sessionId;
        private Dictionary<string, string> sessionValues;

        public SDSession(ulong sessionId)
        {
            this.sessionId = sessionId;
            sessionValues = new Dictionary<string, string>();
        }

        public ulong ID { get { return sessionId; } }

        public void PutValue(string name, string value)
        {
            sessionValues[name] = value;
        }

        public string GetValue(string name)
        {
            if (!sessionValues.ContainsKey(name))
                throw new Exception("Unknown value " + name);

            return sessionValues[name];
        }
    }

    class SessionTable
    {
        private Dictionary<ulong, SDSession> sessionTable;
        private ulong nextSessionId;

        public SessionTable()
        {
            sessionTable = new Dictionary<ulong, SDSession>();
            nextSessionId = 1;
        }
        public SDSession Lookup(ulong Sessionid)
        {
            if (sessionTable.ContainsKey(Sessionid))
            {
                SDSession session = sessionTable[Sessionid];
                return session;
            }
            return null;//no match found
        }
        public void CloseSession(ulong Sessionid)
        {
            if (sessionTable.ContainsKey(Sessionid))
                sessionTable.Remove(Sessionid);//if there is a session with that id remove it
        }
        public SDSession NewSession()
        {
            // allocate a new session, with a unique ID and save it for later in the session table
            ulong sessionId = nextSessionId++;
            SDSession session = new SDSession(sessionId);
            sessionTable[sessionId] = session;
            return session;
        }
    }

  
}
