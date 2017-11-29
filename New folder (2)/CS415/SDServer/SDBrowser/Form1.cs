using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PRSMessageLibrary;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SDBrowser
{
    public partial class Form1 : Form
    {
        static string PRS_ADDRESS = "127.0.0.1";
        static ushort PRS_PORT = 30000;
        Dictionary<String, SDClient> SDSessions = new Dictionary<string, SDClient>();
        public Form1()
        {
            InitializeComponent();
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            if (addressTextBox.Text != null && addressTextBox.Text.Length != 0)
            {
                string address = addressTextBox.Text;
                string[] parts = address.Split(':');
                if (parts.Length == 3)
                {
                    if (parts[0] == "FT")
                    {
                        FTGet(parts[1], parts[2]);
                    }
                    else if (parts[0] == "SD")
                    {
                        SDGet(parts[1], parts[2]);
                    }
                    else
                    {
                        MessageBox.Show("Bad user! Protocol not found!");
                    }
                }
                else
                {
                    MessageBox.Show("Bad user! Invalid Address!");
                }
            }
        }
        private class SDClient
        {
            ulong sessionID;
            Connection Conn;
            public SDClient(string prsaddress, ushort port, string serverip)
            {
                sessionID = 0;
                Conn = new Connection(prsaddress, port, "SD Client", serverip);
            }
            private void Connect()
            {
                Conn.ConnecttoServer();
                string responsestring = null;
                if (sessionID != 0)//We already have a session
                {
                    Conn.SocketWriter.WriteLine("resume");
                    Conn.SocketWriter.WriteLine(sessionID.ToString());
                    responsestring = Conn.SocketReader.ReadLine();
                    if (responsestring == "accepted")
                    {
                        responsestring = Conn.SocketReader.ReadLine();
                        sessionID = System.Convert.ToUInt64(responsestring);
                    }
                    else if (responsestring == "rejected")
                    {
                        responsestring = Conn.SocketReader.ReadLine();
                        throw new Exception("Session resume rejected:" + responsestring);
                    }
                    else
                        throw new Exception("Recieved Invalid Response " + responsestring);
                }
                else
                {
                    Conn.SocketWriter.WriteLine("open");
                    Conn.SocketWriter.Flush();

                    responsestring = Conn.SocketReader.ReadLine();
                    if (responsestring == "accepted")
                    {
                        responsestring = Conn.SocketReader.ReadLine();
                        sessionID = System.Convert.ToUInt64(responsestring);
                    }
                    else
                        throw new Exception("Invlaid response from server " + responsestring);
                }
            }
            private void Disconnect()
            {
                Conn.Disconnect();
            }
            public string Get(string filename)
            {
                string documentContents = null;
                Connect();
                Conn.SocketWriter.WriteLine("get");
                Conn.SocketWriter.WriteLine(filename);
                Conn.SocketWriter.Flush();

                string responsestring = Conn.SocketReader.ReadLine();
                if (responsestring == "success")
                {
                    responsestring = Conn.SocketReader.ReadLine();
                    if (responsestring == filename)
                    {
                        responsestring = Conn.SocketReader.ReadLine();
                        int length = System.Convert.ToInt32(responsestring);

                        char[] buffer = new char[length];
                        int result = Conn.SocketReader.Read(buffer, 0, length);
                        if (result == length)
                        {
                            documentContents = new string(buffer);
                        }
                        else
                            throw new Exception("Error recieved " + result.ToString() + " bytes but expected " + length.ToString() + " bytes");
                    }
                    else
                    {
                        throw new Exception("Recieved unexpecte document name " + responsestring);
                    }
                }
                else if(responsestring == "error")
                {
                    responsestring = Conn.SocketReader.ReadLine();
                    throw new Exception("Error recieved from server: " + responsestring);
                }
                else
                {
                    throw new Exception("Received invalid response " + responsestring);
                }
                Disconnect();
                return documentContents;
            }
            public void CloseSession()
            {
                Conn.ConnecttoServer();
                Conn.SocketWriter.WriteLine(sessionID.ToString());
                Conn.SocketWriter.Flush();

                string responsestring = Conn.SocketReader.ReadLine();
                if(responsestring == "closed")
                {
                    responsestring = Conn.SocketReader.ReadLine();
                    ulong closedsessionid = System.Convert.ToUInt64(responsestring);
                }
                else
                {
                    throw new Exception("Recieved invalid response " + responsestring);
                }
                Disconnect();
            }
        }
        private class Connection
        {
            private string prsip;
            private string ServerIP;
            private ushort prsport;
            private string service_name;
            private Socket sock;
            private NetworkStream socketNetworkStream;
            private StreamReader socketReader;
            private StreamWriter socketWriter;
            public NetworkStream SocketNetworkStream { get { return socketNetworkStream; } }
            public StreamReader SocketReader { get { return socketReader; } }
            public StreamWriter SocketWriter { get { return socketWriter; } }
            public Connection(string prsIP, ushort PRS_PORT, string name, string ServerIP)
            {
                prsip = prsIP;
                this.ServerIP = ServerIP;
                this.prsport = PRS_PORT;
                service_name = name;
            }

            public void ConnecttoServer()
            {
                // get the server port from the PRS for the "FT Server" service
                PRSClient prs = new PRSClient(IPAddress.Parse(prsip), PRS_PORT, "FT Server");
                ushort serverPort = prs.LookupPort();

                // connect to the server on it's IP address and port
                //Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
                Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(IPAddress.Parse(ServerIP), serverPort);
                //Console.WriteLine("Connected to server");

                // establish network stream and reader/writers for the socket
                socketNetworkStream = new NetworkStream(sock);
                socketReader = new StreamReader(socketNetworkStream);
                socketWriter = new StreamWriter(socketNetworkStream);
            }
            public void Disconnect()
            {
                // disconnect from the server and close socket
                Console.WriteLine("Disconnecting from server");
                sock.Disconnect(false);
                socketReader.Close();
                socketWriter.Close();
                socketNetworkStream.Close();
                sock.Close();
                Console.WriteLine("Disconnected from server");
            }

        }


        private void SDGet(string serverIP, string directoryName)
        {
            contentTextBox.Clear();//Clear the text box
            try
            {
                SDClient client = null;
                if (SDSessions.ContainsKey(serverIP))//Session is already started
                {
                    client = SDSessions[serverIP];//Get the session from the session dict

                }
                else//if we dont have a session we need to make a new session
                {
                    client = new SDClient(PRS_ADDRESS, PRS_PORT, serverIP);
                    SDSessions[serverIP] = client;
                }
                string documentcontents = client.Get(directoryName);//Get command on the SDClient
                contentTextBox.AppendText(documentcontents);//Put the text in the textbox
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());//Display error message
            }
        }

        private void FTGet(string serverIP, string directoryName)
        {
            // clear the contents
            contentTextBox.Clear();

            try
            {
                Connection conn = new Connection(PRS_ADDRESS, PRS_PORT, "FT Server", serverIP);

                // send "get\n<directoryName>"
                conn.SocketWriter.WriteLine("get");
                conn.SocketWriter.WriteLine(directoryName);
                conn.SocketWriter.Flush();
                //Console.WriteLine("Sent get " + directoryName);

                // download the files that the server says are in the directory
                bool done = false;
                while (!done)
                {
                    // receive a message from the server
                    //Console.WriteLine("Waiting for msg from server");
                    string cmdString = conn.SocketReader.ReadLine();

                    //if (cmdString.Substring(0, 4) == "done")
                    if (cmdString == "done")
                    {
                        // server is done sending files
                        //Console.WriteLine("Received done");
                        done = true;
                    }
                    else
                    {
                        // server sent us a file name and file length
                        string filename = cmdString;
                        //Console.WriteLine("Received file name from server: " + filename);
                        string lengthstring = conn.SocketReader.ReadLine();
                        int filelength = System.Convert.ToInt32(lengthstring);
                        //Console.WriteLine("Received file length from server: " + filelength.ToString());

                        // read the file contents as a string, and write them to the local file
                        char[] buffer = new char[filelength];
                        int result = conn.SocketReader.Read(buffer, 0, filelength);
                        if (result == filelength)
                        {

                            string fileContents = new string(buffer);
                            contentTextBox.AppendText(filename + "\r\n");
                            contentTextBox.AppendText(fileContents + "\r\n");
                            contentTextBox.AppendText("\r\n");
                            //File.WriteAllText(Path.Combine(directoryName, filename), fileContents);
                            //Console.WriteLine("Wrote " + result.ToString() + " bytes to " + filename + " in " + directoryName);
                        }
                        else
                        {
                            MessageBox.Show("Error: received " + result.ToString() + " bytes, but expected " + filelength.ToString() + " bytes!");
                        }
                    }
                }
                conn.Disconnect();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void Form1_Closing(object sender, EventArgs e)//Form closing
        {
            if(SDSessions.Count > 0)
            {
                foreach(KeyValuePair<string,SDClient> pair in SDSessions)
                {
                    string serverIP = pair.Key;
                    SDClient client = pair.Value;
                    try
                    {
                        client.CloseSession();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Failed to close session to server:" + serverIP + ", error: " + ex.Message);
                    }
                }
            }
        }

        private void contentTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
