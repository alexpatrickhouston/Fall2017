using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace PRSMessageLibrary
{
    public class PRSMessage
    {
        //CONST
        const int MSG_LOCATION = 0;
        const int SERVICE_LOCATION = 1;
        const int PORT_LOCATION = 51;
        const int STATUS_LOCATION = 53;
        public const int SIZE = 54;
        //Variables
        public MsgType msgType;
        public string serviceName;
        public ushort port;
        public Status status;
        public enum MsgType
        {
            REQUEST_PORT = 1,
            LOOKUP_PORT = 2,
            KEEP_ALIVE = 3,
            CLOSE_PORT = 4,
            RESPONSE = 5,
            PORT_DEAD = 6,
            STOP = 7
        }
        public enum Status
        {
            SUCCESS = 0,
            SERVICE_IN_USE = 1,
            SERVICE_NOT_FOUND = 2,
            ALL_PORTS_BUSY = 3,
            INVALID_ARG = 4,
            UNDEFINED_ERROR = 5
        }
        //Static Make Methods
        public static PRSMessage MakeSTOP()
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.STOP;
            return msg;
        }
        public static PRSMessage MakeREQUEST_PORT(string serviceName)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.REQUEST_PORT;
            msg.serviceName = serviceName;
            return msg;
        }
        public static PRSMessage MakeKEEP_ALIVE(string serviceName, ushort port)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.KEEP_ALIVE;
            msg.serviceName = serviceName;
            msg.port = port;
            return msg;
        }
        public static PRSMessage MakeRESPONSE(Status response, ushort port)//response to
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.RESPONSE;
            msg.status = response;
            return msg;
        }
        public static PRSMessage MakePORT_DEAD(ushort port)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.PORT_DEAD;
            msg.port = port;
            return msg;
        }
        public static PRSMessage MakeLOOKUP_PORT(string serviceName)//Sends a message of service name 
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.LOOKUP_PORT;
            msg.serviceName = serviceName;
            return msg;
        }
        //Serialize and Deserialize
        public byte[] Serialize()
        {
            // TODO: Fix the string bug from class
            //return ASCIIEncoding.UTF8.GetBytes("Valid response!!!");
            //MsgType msgType; Single byte
            //string serviceName;
            //int port; --> translate 
            //Status status;
            //First Translate values into network byte order
            ushort shortPort = (ushort)IPAddress.HostToNetworkOrder(port);
            Byte[] buf = new byte[SIZE];
            buf[MSG_LOCATION] = (byte)msgType;
            if (serviceName != null)
            {
                byte[] nameasbytes = ASCIIEncoding.UTF8.GetBytes(serviceName);
                nameasbytes.CopyTo(buf, SERVICE_LOCATION);
            }
            BitConverter.GetBytes(shortPort).CopyTo(buf, PORT_LOCATION);
            buf[STATUS_LOCATION] = (byte)status;
            return buf;
        }
        public static PRSMessage Deserialize(byte[] buffer)
        {
            PRSMessage MSG = new PRSMessage();
            MSG.msgType = (PRSMessage.MsgType)buffer[0];
            MSG.serviceName = new string(ASCIIEncoding.UTF8.GetChars(buffer, SERVICE_LOCATION, 49));
            MSG.port = BitConverter.ToUInt16(buffer, PORT_LOCATION);
            MSG.status = (PRSMessage.Status)buffer[STATUS_LOCATION];
            MSG.port = (ushort)IPAddress.NetworkToHostOrder(MSG.port);
            return MSG;
            //turn bytes into integral values
        }
    }
}

