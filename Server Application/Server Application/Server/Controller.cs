using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server_Application.Server
{
    /// <summary>
    /// This class is the main class of the server. It handles connection requests and assigns ClientHandlers to the connected clients.
    /// It also controls data manupulation on songs and images. It also handles the database connection.
    /// </summary>
    public sealed class Controller
    {
        private Dictionary<string, Socket> _clientStreamingSockets;
        private Dictionary<string, Socket> _clientCommunicationSockets;
        private List<ClientHandler> _clients;
        private readonly int _portCommunication;
        private readonly int _portStreaming;
        private readonly string _host;
        private Thread _listenerCommunicationThread;
        private Thread _listenerStreamingThread;
        private Thread _handleInternalRequests;
        private Task _createClientHandlerTask;
        private Socket _communicationSocket;
        private Socket _streamingSocket;
        private SQLiteConnection _dbConnection;
        private readonly AutoResetEvent _newClient;
        private readonly AutoResetEvent _newInternalRequest;
        private readonly ServerListener _serverListener;
        private readonly Queue<InternalRequest> _internalRequestQueue;
        private readonly object _lock = new object();

        public Controller() : base()
        {
            _clientStreamingSockets = new Dictionary<string, Socket>();
            _clientCommunicationSockets = new Dictionary<string, Socket>();
            _listenerCommunicationThread = new Thread(CommuncationListeningLoop);
            _listenerCommunicationThread.IsBackground = true;
            _listenerStreamingThread = new Thread(StreamingListeningLoop);
            _listenerStreamingThread.IsBackground = true;
            _createClientHandlerTask = new Task(CreateClientHandlerLoop);
            _handleInternalRequests = new Thread(InternalRequestLoop);
            _handleInternalRequests.IsBackground = true;
            _newClient = new AutoResetEvent(false);
            _newInternalRequest = new AutoResetEvent(false);
            _clients = new List<ClientHandler>();
            _host = Config.Config.GetHost();
            _portCommunication = Config.Config.GetPortCommunication();
            _portStreaming = Config.Config.GetPortStreaming();
            _communicationSocket = CreateSocket(_host, _portCommunication);
            _streamingSocket = CreateSocket(_host, _portStreaming);
            _serverListener = new ServerListener();
            _internalRequestQueue = new Queue<InternalRequest>();
            _dbConnection = GetConnection();
            Listen(EventType.InternalRequest, new ServerEventCallback(AddInternalRequest));
        }

        ~Controller()
        {
            _dbConnection.Close();
        }


        private void InternalRequestLoop()
        {
            while (true)
            {

                if (_internalRequestQueue.Count == 0)
                {
                    _newInternalRequest.WaitOne();
                }

                InternalRequest? internalRequest;

                if (_internalRequestQueue.TryDequeue(out internalRequest))
                {
                    ExecuteInternalRequest(internalRequest);
                }
            }
        }

        private string RemoveWhiteSpaces(string input)
        {
            return new string(input.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// This method opens a database connection if the database file exists. If it does not exist,
        /// it creates it, and creates a single table that holds songs and song attributes.
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection GetConnection()
        {
            string path = Config.Config.GetDatabaseRelativePath();
            if (File.Exists(path))
            {
                return new SQLiteConnection($"Data Source={path}; Cashe=Shared").OpenAndReturn();
            }
            else
            {
                SQLiteConnection.CreateFile(path);
                SQLiteConnection connection = new SQLiteConnection($"Data Source={path}; Cashe=Shared");
                connection.Open();
                string sqlCreateTable = "CREATE TABLE songs (songId INTEGER PRIMARY KEY, songName TEXT, artistName TEXT, songNameSerialized TEXT, artistNameSerialized TEXT, duration REAL, songFileName TEXT, imageFileName TEXT)";
                SQLiteCommand command = new SQLiteCommand(sqlCreateTable, connection);
                command.ExecuteNonQuery();
                return connection;
            }
        }

        private void AddInternalRequest(params object[] parameters)
        {
            _internalRequestQueue.Enqueue(new InternalRequest(parameters));
            _newInternalRequest.Set();
        }

        private void ExecuteInternalRequest(InternalRequest internalRequest)
        {
            if (internalRequest.Type == InternalRequestType.AddToDatabase)
            {
                ExecuteAddToDatabaseRequest(internalRequest.Parameters);
            }
            else if (internalRequest.Type == InternalRequestType.RemoveClientHandler)
            {
                ExecuteRemoveClientHandler(internalRequest.Parameters);
            }
        }

        /// <summary>
        /// This method is called to remove a client handler when it is terminated.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteRemoveClientHandler(object[] parameters)
        {
            ClientHandler handler = (ClientHandler)parameters[0];
            _clients.Remove(handler);
            DisplayConnectedClients();
            GC.Collect();
        }

        /// <summary>
        /// This method will add a song to database.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteAddToDatabaseRequest(object[] parameters)
        {
            try
            {
                string songName = (string)parameters[0];
                string artistName = (string)parameters[1];
                string songFilePath = (string)parameters[2];
                string imageFilePath = (string)parameters[3];

                string songNameSerialized = RemoveWhiteSpaces(songName);
                string artistNameSerialized = RemoveWhiteSpaces(artistName);
                double durationSeconds;
                string relativeSongFilePath;
                string relativeImageFilePath;

                CreateAndStoreSongFile(songFilePath, songName, artistName, out relativeSongFilePath, out durationSeconds);
                StoreImageFile(imageFilePath, songName, artistName, out relativeImageFilePath);

                string sqlInsertInto = $@"INSERT INTO songs 
                                        (songName, artistName, songNameSerialized, artistNameSerialized, duration, songFileName, imageFileName)
                                        VALUES
                                        ('{GetSerializedForDatabase(songName)}', 
                                        '{GetSerializedForDatabase(artistName)}', 
                                        '{GetSerializedForDatabase(songNameSerialized)}', 
                                        '{GetSerializedForDatabase(artistNameSerialized)}', 
                                        '{durationSeconds}', 
                                        '{GetSerializedForDatabase(relativeSongFilePath)}', 
                                        '{GetSerializedForDatabase(relativeImageFilePath)}')";
                SQLiteCommand command = new SQLiteCommand(sqlInsertInto, _dbConnection);
                command.ExecuteNonQuery();
            }

            catch (Exception)
            {
            }
        }
        
        /// <summary>
        /// If there is a "'" in any of the strings, it will add an additional "'" so that it does not cause errors
        /// with the database.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetSerializedForDatabase(string input)
        {
            List<char> result = new List<char>();

            foreach (char c in input)
            {
                if (c.Equals('\''))
                {
                    result.Add('\'');
                    result.Add(c);
                }
                else
                {
                    result.Add(c);
                }
            }

            return new string(result.ToArray());
        }

        private void StoreImageFile(string imageFilePath, string songName, string artistName, out string relativeFilePath)
        {
            string fileName = $"{songName} by {artistName}.png";
            string relativeFile = $"{Config.Config.GetImageFilesRelativePath()}{fileName}";
            byte[] bytes = File.ReadAllBytes(imageFilePath);
            File.WriteAllBytes(relativeFile, bytes);
            relativeFilePath = relativeFile;
        }

        private double GetDurationSeconds(byte[] header)
        {
            int bytesPerSecond = BitConverter.ToInt32(header.Take(new Range(28, 32)).ToArray());
            int dataSize = BitConverter.ToInt32(header.Take(new Range(40, 44)).ToArray());

            return (double)dataSize / bytesPerSecond;
        }

        private void CreateAndStoreSongFile(string songFilePath, string songName, string artistName, out string relativeFilePath, out double durationSeconds)
        {
            string fileName = $"{songName} by {artistName}.bytes";
            string relativeFile = $"{Config.Config.GetAudioFilesRelativePath()}{fileName}";
            byte[] header;
            WriteAudioBytes(songFilePath, relativeFile, out header);
            durationSeconds = GetDurationSeconds(header);

            relativeFilePath = relativeFile;
        }

        /// <summary>
        /// This method will read a wave file, standardize it, split it into packets, and store it.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="relativeFilePath"></param>
        /// <param name="header"></param>
        private void WriteAudioBytes(string filePath, string relativeFilePath, out byte[] header)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            StandardWaveBuilder standardWaveBuilder = new StandardWaveBuilder(bytes);
            byte[] standardWave = standardWaveBuilder.GetStandardWave();
            Array.Clear(bytes);
            byte[] audioFile = AudioFile.GetAudioBytes(standardWave);
            header = audioFile.Take(44).ToArray();
            File.WriteAllBytes(relativeFilePath, audioFile);
        }


        private void Listen(EventType eventType, ServerEventCallback serverEventCallback)
        {
            _serverListener.Listen(eventType, serverEventCallback);
        }

        /// <summary>
        /// Listens for clients trying to connect to the streaming port.
        /// </summary>
        private void StreamingListeningLoop()
        {
            while (true)
            {
                Socket clientSocket = _streamingSocket.Accept();
                IPEndPoint? ipEndPoint = (IPEndPoint?)clientSocket.RemoteEndPoint;
                string clientId = Encoding.UTF8.GetString(ReceiveTCP(6, clientSocket));
                string clientFullId = GetClientFullId(clientId, ipEndPoint);
                lock (_lock)
                {
                    _clientStreamingSockets.Add(clientFullId, clientSocket);
                    _newClient.Set();
                }
            }
        }

        /// <summary>
        /// Listens for clients trying to connect to communication port.
        /// </summary>
        private void CommuncationListeningLoop()
        {
            while (true)
            {
                Socket clientSocket = _communicationSocket.Accept();
                IPEndPoint? ipEndPoint = (IPEndPoint?)clientSocket.RemoteEndPoint;
                string clientId = Encoding.UTF8.GetString(ReceiveTCP(6, clientSocket));
                string clientFullId = GetClientFullId(clientId, ipEndPoint);
                lock (_lock)
                {
                    _clientCommunicationSockets.Add(clientFullId, clientSocket);
                    _newClient.Set();
                }
            }
        }

        /// <summary>
        /// Creates a client handler after two sockets from the same client are received - streaming and communication.
        /// </summary>
        private void CreateClientHandlerLoop()
        {
            while (true)
            {
                _newClient.WaitOne();
                lock (_lock)
                {
                    _newClient.Reset();
                    foreach (var key in _clientStreamingSockets.Keys)
                    {
                        if (_clientCommunicationSockets.ContainsKey(key))
                        {
                            _clients.Add(new ClientHandler(key, 
                                _clientStreamingSockets[key], 
                                _clientCommunicationSockets[key], 
                                _dbConnection));

                            _clientCommunicationSockets.Remove(key);
                            _clientStreamingSockets.Remove(key);

                            DisplayConnectedClients();
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Asks the window to display the connected clients.
        /// </summary>
        private void DisplayConnectedClients()
        {
            List<string> connectedClients = new List<string>();
            foreach (var client in _clients)
            {
                connectedClients.Add(client.ClientId);
            }
            new ServerEvent(EventType.DisplayConnectedClients, true, connectedClients);
        }

        private string GetClientFullId(string ClientId, IPEndPoint? iPEndPoint)
        {
            if (iPEndPoint != null)
            {
                return $"{ClientId}@{iPEndPoint.Address.ToString()}";
            }
            return $"{ClientId}@";
        }

        /// <summary>
        /// Starts the controller and the sever.
        /// </summary>
        public void Run()
        {
            _listenerCommunicationThread.Start();
            _listenerStreamingThread.Start();
            _createClientHandlerTask.Start();
            _handleInternalRequests.Start();
        }

        private Socket CreateSocket(string ipAddress, int port)
        {
            IPAddress IP = IPAddress.Parse(ipAddress);
            IPEndPoint ipe = new IPEndPoint(IP, port);
            Socket socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipe);
            socket.Listen(100);
            return socket;
        }

        /// <summary>
        /// Makes sure all the data is received.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="socket"></param>
        /// <returns></returns>
        /// <exception cref="SocketException"></exception>
        private byte[] ReceiveTCP(int size, Socket socket)
        {
            byte[] packet = new byte[size];
            int bytesReceived = 0;
            int x;
            while (bytesReceived < size)
            {
                byte[] buffer = new byte[size - bytesReceived];
                x = socket.Receive(buffer);
                if (x == 0)
                {
                    throw new SocketException();
                }
                Buffer.BlockCopy(buffer, 0, packet, bytesReceived, x);
                bytesReceived += x;
            }
            return packet;
        }
    }
}
