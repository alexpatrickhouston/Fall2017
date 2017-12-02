using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace PRSMessageLibrary
{
        public class PRSServiceClient
        {
            public static IPAddress prsAddress;
            public static ushort prsPort;
        private IPAddress iPAddress;
        private ushort pRS_PORT;
        private string v;

        public PRSServiceClient(string serviceName)
            {
                // TODO: PRSServiceClient.PRSServiceClient()
            }

        public PRSServiceClient(IPAddress iPAddress, ushort pRS_PORT, string v)
        {
            this.iPAddress = iPAddress;
            this.pRS_PORT = pRS_PORT;
            this.v = v;
        }

        public ushort LookupPort()
            {
                return 40001;
            }
            public ushort RequestPort()
            {
                // TODO: PRSServiceClient.RequestPort()
                // after successfully requesting a port
                // this class will keep the port alive on a separate thread
                // until the port closed
                return 40001;
            }

            public void ClosePort()
            {
                // TODO: PRSServiceClient.ClosePort()
            }

            private void KeepAlive()
            {
                // TODO: PRSServiceClient.KeepAlive()
            }
        }
    }

