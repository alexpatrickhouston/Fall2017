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
        class ManagedPort
        {
            public int port;//The port number
            public bool reserved;//Whether it is in use or not
            public string serviceName;//Name of the service
            public DateTime lastAlive;//When it was last alive
            public bool PortAlive(int timeout)
            {
                //If the port is reserved or last alive is smaller than timeout port is alive otherwise return false;
                if (reserved || ((DateTime.Now - lastAlive).TotalSeconds) < timeout)
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
                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        PRSMessage msg = PRSCommunicator.ReceiveMessage(listeningSocket, ref remoteEP);
                        if (msg.msgType == PRSMessage.MsgType.STOP)
                        {
                            done = true;
                        }
                        PRSMessage response = Handler.ParseMessageType(msg);
                        PRSCommunicator.SendMessage(listeningSocket, remoteEP, response);
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
        class PRSHandler//Handles the messages recieved
        {
            //Variables
            int startingClientPort;//starting port
            int endingClientPort;//ending port <inclusive>
            int keepAlive;//keep alive value
            List<ManagedPort> Ports;//list of ports

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
            public PRSMessage ParseMessageType(PRSMessage Request)//Parses through message and formulates a response to be sent back
            {
                //error checking
                //0. SUCCESS
                //1.SERVICE_IN_USE
                //2.SERVICE_NOT_FOUND
                //3.ALL_PORTS_BUSY
                //4.INVALID_ARG
                //5.UNDEFINED_ERROR
                try
                {
                    switch (Request.msgType)//checks the type
                    {
                        case PRSMessage.MsgType.PORT_DEAD:
                            return portdead(Request);
                        case PRSMessage.MsgType.CLOSE_PORT://Closes the requested port 3 errors 2,4,5
                                                           //if service in msg is not at either the correct port or name
                                                           //throw out Response service not found
                            return close_port(Request);

                        case PRSMessage.MsgType.KEEP_ALIVE://keeps service alive 3 errors 2,4,5
                                                           //if port or service is incorrect return invalid
                            return keep_alive(Request);

                        case PRSMessage.MsgType.LOOKUP_PORT://takes a service name and gives a port error 2,4,5
                                                            //if not well formulated throw invalid
                                                            //if service name not correct return not found
                            return lookup_port(Request);

                        case PRSMessage.MsgType.REQUEST_PORT://Gives service lowest port 1,3,4,5
                            return request_port(Request);

                        case PRSMessage.MsgType.STOP://Server gets stopped by the Handler this sends response
                                                     //only checks message type 
                            Console.WriteLine("Stop Has been recieved");
                            return PRSMessage.MakeSTOP();//Stop doesn't impact the port reservations so it just uses its method
                        default://if not a correct message code
                            return PRSMessage.MakeRESPONSE(PRSMessage.Status.INVALID_ARG, 0);//Returns an invalid arg
                    }
                }
                catch (Exception e)
                {
                    return PRSMessage.MakeRESPONSE(PRSMessage.Status.UNDEFINED_ERROR, 0);//if this happened the service broke
                }

            }
            private PRSMessage portdead(PRSMessage Request)
            {
                ManagedPort p = find_port(Request.port);
                p.reserved = false;
                Console.WriteLine("Marked port dead:" + p.port);
                return PRSMessage.MakeRESPONSE(PRSMessage.Status.SUCCESS, Request.port);
            }
            private PRSMessage close_port(PRSMessage Request)
            {
                //check if port and name are accurate
                ManagedPort n = find_service(Request.serviceName);//checks if the service is valid
                if (n == null)
                    return PRSMessage.MakeRESPONSE(PRSMessage.Status.SERVICE_NOT_FOUND, Request.port);
                ManagedPort p = find_port(Request.port);//Checks if the port is valid
                if (p == null || p != n)
                    return PRSMessage.MakeRESPONSE(PRSMessage.Status.INVALID_ARG, Request.port);
                //Actually close port
                p.serviceName = null;
                p.reserved = false;
                return PRSMessage.MakeRESPONSE(PRSMessage.Status.SUCCESS, Request.port);
            }
            private PRSMessage keep_alive(PRSMessage Request)
            {
                //check if the service and port are both valid and match
                ManagedPort n = find_service(Request.serviceName);//checks if the service is valid
                if (n == null)
                    return PRSMessage.MakeRESPONSE(PRSMessage.Status.SERVICE_NOT_FOUND, Request.port);
                ManagedPort p = find_port(Request.port);//Checks if the port is valid
                if (p == null || p != n)
                    return PRSMessage.MakeRESPONSE(PRSMessage.Status.INVALID_ARG, Request.port);
                p.lastAlive = DateTime.Now;
                return PRSMessage.MakeRESPONSE(PRSMessage.Status.SUCCESS, Request.port);
            }
            private PRSMessage lookup_port(PRSMessage Request)
            {
                ManagedPort p = find_service(Request.serviceName);//find the service in the list
                if (p == null)
                    return PRSMessage.MakeRESPONSE(PRSMessage.Status.SERVICE_NOT_FOUND, Request.port);//if the service isnt in the list return not found
                return PRSMessage.MakeRESPONSE(PRSMessage.Status.SUCCESS, (ushort)p.port);//if it is return success with port number

            }
            private PRSMessage request_port(PRSMessage Request)
            {
                //check if service in use
                ManagedPort sn = find_service(Request.serviceName);//if the service is in the list
                if (sn != null)//check to see if it was found in the list
                    return PRSMessage.MakeRESPONSE(PRSMessage.Status.SERVICE_IN_USE, Request.port);//if it was its already in use
                foreach (ManagedPort p in Ports)//for each port in the list
                {
                    if (!p.reserved && p.serviceName == null)//check if its reserved
                    {
                        p.reserved = true;//reserve it
                        p.serviceName = Request.serviceName;//set the service name
                        p.lastAlive = DateTime.Now;//update the time
                        return PRSMessage.MakeRESPONSE(PRSMessage.Status.SUCCESS, (ushort)p.port);//return success with port number
                    }
                }
                return PRSMessage.MakeRESPONSE(PRSMessage.Status.ALL_PORTS_BUSY, Request.port);
            }
            private ManagedPort find_port(int portnumber)
            {
                foreach (ManagedPort p in Ports)
                {
                    if (!p.PortAlive(keepAlive))//if it is outside the keep alive close the port
                    {
                        p.serviceName = null;
                        p.reserved = false;
                    }
                    if (p.port == portnumber)
                        return p;
                }
                return null;
            }
            private ManagedPort find_service(string servicename)
            {
                foreach (ManagedPort p in Ports)
                {
                    if (p.serviceName == servicename)
                        return p;
                }
                return null;
            }
        }
        static void Main(string[] args)
        {
            int ListeningPort = PRSCommunicator.DEFAULT_PORT;
            int StartingClientPort = PRSCommunicator.DEFAULT_STARTING;
            int EndingClientPort = PRSCommunicator.DEFAULT_ENDING;
            int Timeout = PRSCommunicator.DEFAULT_TIMEOUT;
            PRSListener PRS = new PRSListener();

            if (args != null)
            {
                string[] command = args;

                for (int i = 0; i < command.Length; i = i + 2)
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

            }

            else
            {
                PRS.StartService(ListeningPort, StartingClientPort, EndingClientPort, Timeout);
            }
        }
    }
}







