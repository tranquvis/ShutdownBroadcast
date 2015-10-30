using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace ClientApp
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SwHide = 0;
        const int SwShow = 5;

        static string _serverIp = "";
        static string _secret = "";
        static int _port;

        static IntPtr _handle;
        static StreamWriter _logSw;

        static void Main(string[] args)
        {
            _handle = GetConsoleWindow();
            ShowWindow(_handle, SwHide); //replace SwHide with SwShow to show console window

            #region check autostart registry key

            Directory.SetCurrentDirectory(Application.ExecutablePath.Remove(Application.ExecutablePath.LastIndexOf('\\')));
            
            // path to the key where Windows looks for startup applications
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rkApp == null)
                WriteLog("Error while loading autostart registry key.");

            if (rkApp != null && !IsStartupItem(rkApp))
            {
                // add the value in the registry so that the application runs at startup
                rkApp.SetValue("ShutdownBroadcastListener", Application.ExecutablePath);
                WriteLog("The registry key was set successfully. The client will start automatically on next pc start.");
            }

            #endregion

            #region load configuration

            _logSw = new StreamWriter("log.txt", true);

            try
            {
                LoadData();
            }
            catch (IOException)
            {
                WriteLog("Error while loading configuartion: config.txt wasn't found");
                _logSw?.Close();
                return;
            }
            catch (ApplicationException ae)
            {
                WriteLog(ae.Message);
                _logSw?.Close();
                return;
            }

            _logSw?.Close();

            WriteLog("configuation loaded successfully");
            WriteLog("server ip = " + _serverIp);
            WriteLog("secret = " + _secret);
            WriteLog("port = " + _port);

            #endregion
            
            BeginReceive();

            //close application when "cancel" is entered
            while (true)
            {
                string l = Console.ReadLine();
                if (l == "cancel")
                    break;
            }
        }
        
        /// <summary>
        /// start listening for shutdown request in new thread
        /// </summary>
        private static void BeginReceive()
        {
            new Thread(() =>
            {
                _logSw = new StreamWriter("log.txt", true);

                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _port);
                try
                {
                    listener.Bind(localEndPoint);
                }
                catch(SocketException se)
                {
                    WriteLog(se.Message);
                    _logSw.Close();
                    Application.Exit();
                    return;
                }
                EndPoint ep = localEndPoint;

                byte[] data = new byte[1024];
                WriteLog("Waiting for shutdown request...");
                var recv = listener.ReceiveFrom(data, ref ep);

                string stringData = Encoding.ASCII.GetString(data, 0, recv);
                WriteLog($"Request received with secret {stringData} from {_serverIp}");
                listener.Close();

                if (stringData.Equals(_secret))
                    Shutdown();
                else{
                    WriteLog("The received secret doesn't match with the configuartion.\n note: the secrets on client an server must be equal");
                    _logSw.Close();
                    Application.Exit();
                    return;
                }

                _logSw.Close();
            }).Start();
        }

        /// <summary>
        /// check if the registry entry for autostart is set and is equal to the current executable path
        /// </summary>
        /// <param name="rkApp">registry key</param>
        /// <returns></returns>
        private static bool IsStartupItem(RegistryKey rkApp)
        {
            string regKey = (string)rkApp.GetValue("ShutdownBroadcastListener");
            return regKey != null && regKey == Application.ExecutablePath;
        }

        /// <summary>
        /// load configuration from config file
        /// </summary>
        private static void LoadData()
        {
            StreamReader sr = new StreamReader(Directory.GetCurrentDirectory()+"/config.txt");
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split('=');
                switch (parts[0].Trim())
                {
                    case "server_ip":
                        _serverIp = parts[1].Replace('"',' ').Trim();
                        break;
                    case "secret":
                        int start = parts[1].IndexOf('"');
                        int end = parts[1].LastIndexOf('"')-1;
                        _secret = parts[1].Substring(start + 1, end - start);
                        break;
                    case "port":
                        _port = Convert.ToInt16(parts[1].Replace('"', ' ').Trim());
                        break;
                }
            }
            sr.Close();

            if (_port == 0 || _serverIp == "" || _secret == "")
                throw new ApplicationException("Error while loading configuration: One or more arguments are not set in the configuration file.");
        }

        /// <summary>
        /// write text to console window and log file
        /// </summary>
        /// <param name="text"></param>
        private static void WriteLog(string text)
        {
            Console.WriteLine(text);
            _logSw.WriteLine($"{DateTime.Now}: {text}");
        }

        /// <summary>
        /// shutdown pc
        /// </summary>
        private static void Shutdown()
        {
            Process.Start("shutdown","/s /t 0");
        }
    }
}
