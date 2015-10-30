using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ServerApp
{
    class Program
    {
        static string _secret = "";
        static int _port;
        static string _bcIp = "";

        static void Main(string[] args)
        {
            #region load configuration
            try
            {
                LoadData();
            }
            catch (IOException)
            {
                Console.WriteLine("Error while loading configuration: config.txt wasn't found");
                Console.ReadKey();
                return;
            }
            catch(ApplicationException ae)
            {
                Console.WriteLine(ae.Message);
                Console.ReadKey();
                return;
            }
            Console.WriteLine("configuration loaded successfully");
            Console.WriteLine("broadcast ip = " + _bcIp);
            Console.WriteLine("secret = " + _secret);
            Console.WriteLine("port = " + _port);
            #endregion

            #region send shutdown request
            try
            {
                SendBroadcast(System.Text.Encoding.UTF8.GetBytes(_secret), 8888);
            }
            catch(Exception)
            {
                Console.WriteLine("An unknown error occured while sending broadcast.");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("shutdown has been successfully requested");
            #endregion
        }

        /// <summary>
        /// load configuration from config file
        /// </summary>
        private static void LoadData()
        {
            StreamReader sr = new StreamReader("config.txt");
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split('=');
                switch (parts[0].Trim())
                {
                    case "broadcast_ip":
                        _bcIp = parts[1].Replace('"', ' ').Trim();
                        break;
                    case "secret":
                        int start = parts[1].IndexOf('"');
                        int end = parts[1].LastIndexOf('"') - 1;
                        _secret = parts[1].Substring(start + 1, end - start);
                        break;
                    case "port":
                        _port = Convert.ToInt16(parts[1].Replace('"', ' ').Trim());
                        break;
                }
            }
            sr.Close();

            if (_port == 0 || _bcIp == "" || _secret == "")
                throw new ApplicationException("Error while loading configuration: One or more arguments are not set in the configuration file.");
        }

        /// <summary>
        /// send shutdown broadcast in local network
        /// </summary>
        /// <param name="data">data that should be attached to the broadcast</param>
        /// <param name="port">destination port</param>
        public static void SendBroadcast(byte[] data, int port)
        {
            SendData(data, IPAddress.Parse(_bcIp), port);
        }

        /// <summary>
        /// send data to clients in local network
        /// </summary>
        /// <param name="data">data that should be attached to the broadcast</param>
        /// <param name="ip">destination ip</param>
        /// <param name="port">destination port</param>
        public static void SendData(byte[] data, IPAddress ip, int port)
        {
            //define socket
            Socket bcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //define EndPoint or request destination
            IPEndPoint iep1 = new IPEndPoint(ip, port);

            //bind options to socket
            bcSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0);

            //send broadcast
            bcSocket.SendTo(data, iep1);

            bcSocket.Close();
        }
    }
}
