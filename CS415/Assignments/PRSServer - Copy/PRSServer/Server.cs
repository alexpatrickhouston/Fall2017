using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using PRSMessageLibrary;
using System.Linq;
using System.Net.Sockets;
namespace PRSServer//LIstener and actual PRS
{
    class Server
    {
        const int DEFAULT_PORT = 30000;
        const int DEFAULT_STARTING = 40000;
        const int DEFAULT_ENDING = 40099;
        const int DEFAULT_TIMEOUT = 300;
        class ManagedPort
        {
            public int port;//The port number
            public bool reserved;//Whether it is in use or not
            public string serviceName;//Name of the service
            public DateTime lastAlive;//When it was last alive
            public bool PortAlive(int timeout)
            {
                //If the port is reserved or last alive is smaller than timeout port is alive otherwise return false;
                if (reserved || ((DateTime.Now-lastAlive).TotalSeconds) < timeout)
                    return true;
                return false;
            }
        }
        class PRSListener
        {

            int servicePort;
            PRSHandler Handler;//Handles the messages recieved

            public void StartService(int P, int S, int E, int T)
            {
                servicePort = P;//Set the Service Port to listen on
                Handler = new PRSHandler(S, E, T);//Create the handler to deal with the ports
                Socket listeningSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                Console.WriteLine("Listening socket created");

                // bind the socket to the server port
                listeningSocket.Bind(new IPEndPoint(IPAddress.Any, servicePort));
                Console.WriteLine("Listening socket bound to port " + servicePort.ToString());
                bool done = false;//For stop message
                while (!done)// a stop message has not been recieved
                {
                    try
                    {
                        // receive a message from a client
                        Console.WriteLine("Waiting for message from client...");
                        byte[] buffer = new byte[PRSMessage.SIZE];
                        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        int result = listeningSocket.ReceiveFrom(buffer, ref remoteEP);
                        Console.WriteLine("Received " + result.ToString() + " bytes: " + new string(ASCIIEncoding.UTF8.GetChars(buffer)));
                        PRSMessage msg = PRSMessage.Deserialize(buffer);
                        if(msg.msgType == PRSMessage.MsgType.STOP)
                        {
                            done = true;
                        }
                        PRSMessage response = Handler.ParseMessage(msg);
                        PRSCommunicator.SendMessage(listeningSocket, remoteEP, response );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception when receiving..." + ex.Message);
                    }
                }

                // close the socket and quit
                Console.WriteLine("Closing down");
                listeningSocket.Close();
                Console.WriteLine("Closed!");

                Console.ReadKey();
            }

        }
        class PRSHandler
        {
            //Variables
            int startingClientPort;
            int endingClientPort;
            int keepAlive;
            List<ManagedPort> Ports;

            public PRSHandler(int S, int E, int T)//Constructor
            {
                startingClientPort = S;//sets the starting clientport
                endingClientPort = E;//sets the ending clientport
                keepAlive = T;//sets the keep alive time
                Ports = new List<ManagedPort>();//creates a new list of ports
                for (int p = startingClientPort; p <= endingClientPort; p++)//fills the list
                {
                    ManagedPort mp = new ManagedPort();
                    mp.port = p;
                    mp.reserved = false;
                    
                    Ports.Add(mp);
                }
            }
            public PRSMessage ParseMessage(PRSMessage Request)//Parses through message and formulates a response to be sent back
            {
                //error checking0. SUCCESS
                //1.SERVICE_IN_USE
                //2.SERVICE_NOT_FOUND
                //3.ALL_PORTS_BUSY
                //4.INVALID_ARG
                //5.UNDEFINED_ERROR
                switch (Request.msgType)
                {
                    case PRSMessage.MsgType.CLOSE_PORT://Closes the requested port 3 errors 2,4,5
                        return PRSMessage.MakeRESPONSE(0, 0);//Returns a success

                    case PRSMessage.MsgType.KEEP_ALIVE://keeps service alive 3 errors 2,4,5
                        return PRSMessage.MakeRESPONSE(0, 0);//Returns a success

                    case PRSMessage.MsgType.LOOKUP_PORT://takes a service name and gives a port error 2,4,5
                        return PRSMessage.MakeRESPONSE(0, 0);//Returns a success

                    case PRSMessage.MsgType.REQUEST_PORT://Gives service lowest port 2,3,4,5
                        return PRSMessage.MakeRESPONSE(0, 0);//Returns a success

                    case PRSMessage.MsgType.STOP://Server gets stopped by the Handler this sends response
                        return PRSMessage.MakeRESPONSE(0, 0);//Returns a success
                    default:
                        return PRSMessage.MakeRESPONSE(0, 0);//Returns a success;
                }

            }
        }
        static void Main(string[] args)
        {
            int ListeningPort=DEFAULT_PORT;
            int StartingClientPort=DEFAULT_STARTING;
            int EndingClientPort=DEFAULT_ENDING;
            int Timeout=DEFAULT_TIMEOUT;
            PRSListener PRS = new PRSListener();
            bool end = false;
            while (!end)
            {
                Console.WriteLine("Please Enter your Command");
                string user_command = Console.ReadLine();
                string[] command = user_command.Split();
                switch (command[0])
                {
                    case "end":
                        end = true;
                        break;
                    case "help":
                        Console.WriteLine("Use: prs\n options:\n -p <service port>\n -s < starting client port number >\n -e < ending client port number >\n -t < keep alive time in seconds >\n");
                        break;
                    case "prs"://if they want to start the service
                        try
                        {
                            for (int i = 1; i < command.Length; i = i + 2)
                            {
                                switch (command[i].ToLower())
                                {
                                    case "-p":
                                        ListeningPort = Convert.ToInt32(command[i + 1]);
                                        break;
                                    case "-s":
                                        StartingClientPort = Convert.ToInt32(command[i + 1]);
                                        break;
                                    case "-e":
                                        EndingClientPort = Convert.ToInt32(command[i + 1]);
                                        break;
                                    case "-t":
                                        Timeout = Convert.ToInt32(command[i + 1]);
                                        break;
                                }

                            }
                            if (StartingClientPort > EndingClientPort)
                            {
                                throw (new Exception("StartingPort larger than ending port"));
                            }
                            PRS.StartService(ListeningPort, StartingClientPort, EndingClientPort, Timeout);
                            break;
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine("Exception occured:" + E);
                        }
                        break;
                    default:
                        Console.WriteLine("Invalid Input");
                        break;
                }
            }

        }


    }
}
