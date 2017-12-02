﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PRSProtocolLibrary;


namespace PRSServer
{
    class ServerProgram
    {
        class ManagedPort
        {
            /*
            port #
	        currently reserved(or not)
            service name
            when it was last alive(either reserved or keep - alive)
            */

            public int port;
            public bool reserved;
            public string serviceName;
            public DateTime lastAlive;
        }

        static void Main(string[] args)
        {
            // TODO: interpret cmd line options
            /*
            -p <service port>
            -s <starting client port number>
            -e <ending client port number>
            -t <keep alive time in seconds>
            */

            int servicePort = 30000;
            int startingClientPort = 40000;
            int endingClientPort = 40099;
            int keepAlive = 300;

            // initialize a collection of un-reserved ports to manage
            List<ManagedPort> ports = new List<ManagedPort>();
            for (int p = startingClientPort; p <= endingClientPort; p++)
            {
                ManagedPort mp = new ManagedPort();
                mp.port = p;
                mp.reserved = false;

                ports.Add(mp);
            }

            // create the socket for receiving messages at the server
            Socket listeningSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Console.WriteLine("Listening socket created");

            // bind the socket to the server port
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, servicePort));
            Console.WriteLine("Listening socket bound to port " + servicePort.ToString());

            // listen for client messages
            bool done = false;
            while (!done)
            {
                try
                {
                    // receive a message from a client
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    PRSMessage msg = PRSCommunicator.ReceiveMessage(listeningSocket, ref remoteEP);

                    // handle the message
                    PRSMessage response = null;
                    switch (msg.msgType)
                    {
                        case PRSMessage.MsgType.REQUEST_PORT:
                            Console.WriteLine("Received REQUEST_PORT message");
                            response = Handle_REQUEST_PORT(msg);
                            break;

                        case PRSMessage.MsgType.STOP:
                            Console.WriteLine("Received STOP message");
                            done = true;
                            break;

                        default:
                            // TODO: handle unknown message type!
                            break;
                    }

                    if (response != null)
                    {
                        // send response message back to client
                        PRSCommunicator.SendMessage(listeningSocket, remoteEP, response);
                    }
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

        private static PRSMessage Handle_REQUEST_PORT(PRSMessage msg)
        {
            /*
            validate msg arguments...
	        if service name already has a reserved port
		        return SERVICE_IN_USE
	        if otherwise invalid
		        return INVALID_ARG
        find lowest numbered unused port
	        reserve the chosen port
	        set last alive to now
	        return SUCCESS and the chosen port
        if no port available
	        return ALL_PORTS_BUSY
        if error occurs
	        return UNDEFINED_ERROR
            */

            // return expected response type message
            return new PRSMessage();     
        }
    }
}
