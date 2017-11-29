using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PRSMessageLibrary
{
    public class PRSClient
    {

        public PRSClient(IPAddress PRS_ADDRESS, ushort PRS_PORT,  string SERVICE_NAME)
        {

        }
        public ushort LookupPort()
        {
            return 40000;
        }
        public ushort RequestPort()
        {
            return 40000;
        }
        public void ClosePort()
        {

        }
    }
}
