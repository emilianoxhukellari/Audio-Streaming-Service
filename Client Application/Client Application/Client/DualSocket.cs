using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    /// <summary>
    /// This class represents a dual socket design. One socket is dedicated to MediaPlayer, and the other to Controller.
    /// If the Dual Socket receives a Reconnect() command, it will reconnect both sockets.
    /// </summary>
    public sealed class DualSocket
    {
        public Socket MediaPlayerSocket { get; set; }
        public Socket ControllerSocket { get; set; }
        public bool Connected { get; set; }
        private string _clientId;
        private IPEndPoint _controllerIPE;
        private IPEndPoint _mediaPlayerIPE;
        public ManualResetEvent ReconnectFlag { get; set; }

        private readonly Task _connectionTaskController;
        private readonly Task _connectionTaskMediaPlayer;

        public DualSocket(IPEndPoint controllerIPE, IPEndPoint mediaPlayerIPE, string clientId)
        {
            _mediaPlayerIPE = mediaPlayerIPE;
            _controllerIPE = controllerIPE;
            MediaPlayerSocket = new Socket(_mediaPlayerIPE.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ControllerSocket = new Socket(_controllerIPE.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ReconnectFlag = new ManualResetEvent(false);
            _controllerIPE = controllerIPE;
            _mediaPlayerIPE = mediaPlayerIPE;
            _clientId = clientId;

            _connectionTaskMediaPlayer = new Task(ConnectToServerMediaPlayer);
            _connectionTaskController = new Task(ConnectToServerController);
            _connectionTaskMediaPlayer.Start();
            _connectionTaskController.Start();
            ReconnectFlag.Set();
        }

        ~DualSocket()
        {
            MediaPlayerSocket.Shutdown(SocketShutdown.Both);
            MediaPlayerSocket.Close();
            ControllerSocket.Shutdown(SocketShutdown.Both);
            ControllerSocket.Close();
        }

        public void Reconnect()
        {
            try
            {
                MediaPlayerSocket.Shutdown(SocketShutdown.Both);
                MediaPlayerSocket.Close();
            }
            catch { }
            try
            {
                ControllerSocket.Shutdown(SocketShutdown.Both);
                ControllerSocket.Close();
            }
            catch { }
            ReconnectFlag.Set();
        }

        private void ConnectToServerController()
        {
            while (true)
            {
                ReconnectFlag.WaitOne();
                ControllerSocket = new Socket(_controllerIPE.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                while (true)
                {
                    try
                    {
                        Connected = false;
                        ControllerSocket.Connect(_controllerIPE);
                        ClientTCP.SendTCP(Encoding.UTF8.GetBytes(_clientId), 6, ControllerSocket);
                        Connected = true;
                        break;
                    }
                    catch (SocketException)
                    {
                    }
                }
                ReconnectFlag.Reset();
            }
        }

        private void ConnectToServerMediaPlayer()
        {
            while (true)
            {
                ReconnectFlag.WaitOne();
                MediaPlayerSocket = new Socket(_mediaPlayerIPE.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                while (true)
                {
                    try
                    {
                        Connected = false;
                        MediaPlayerSocket.Connect(_mediaPlayerIPE);
                        ClientTCP.SendTCP(Encoding.UTF8.GetBytes(_clientId), 6, MediaPlayerSocket);
                        Connected = true;
                        break;
                    }
                    catch (SocketException)
                    {
                    }
                }
                ReconnectFlag.Reset();
            }
        }
    }
}
