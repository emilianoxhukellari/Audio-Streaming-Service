using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server_Application.Server
{
    /// <summary>
    /// This class represents a handler for a single client. It contains the necessary logic and 
    /// attributes to communicate with a client.
    /// </summary>
    public sealed class ClientHandler
    {
        private readonly Socket _streamingSocket;
        private readonly Socket _communcationSocket;
        private readonly SQLiteConnection _dbConnection;
        public string ClientId { get; }
        private bool _streamingLoopEnded;
        private bool _communicationLoopEnded;
        private Thread _streamingThread;
        private Thread _communicationThread;
        private readonly object _lock = new object();

        private bool _stopSend;

        private readonly byte[] DATA = Encoding.UTF8.GetBytes("data");
        private readonly byte[] EXIT = Encoding.UTF8.GetBytes("exit");
        private readonly byte[] MOD1 = Encoding.UTF8.GetBytes("mod1"); // Regular streaming
        private readonly byte[] MOD2 = Encoding.UTF8.GetBytes("mod2"); // Optimization streaming
        private byte[] _lastSongData;

        public ClientHandler(string clientId, Socket streamingSocket, Socket communicationSocket, SQLiteConnection dbConnection)
        {
            ClientId = clientId;
            _lastSongData = new byte[0];
            _streamingSocket = streamingSocket;
            _communcationSocket = communicationSocket;
            _dbConnection = dbConnection;
            _streamingLoopEnded = false;
            _communicationLoopEnded = false;
            _streamingThread = new Thread(StreamingLoop);
            _streamingThread.IsBackground = true;
            _communicationThread = new Thread(CommuncationLoop);
            _communicationThread.IsBackground = true;
            _stopSend = false;
            Run();
        }

        /// <summary>
        /// This method will send a range of packets to the client. It can be interrupted if
        /// _stopSend is set to false.
        /// </summary>
        /// <param name="fromPacket"></param>
        /// <param name="toPacket"></param>
        private void SendSongData(int fromPacket, int toPacket)
        {
            byte[] songData = _lastSongData;

            SendTCP(MOD2, 4, _streamingSocket);
            int fromPacketOffset = (fromPacket / 4092) * 4 + 48;
            int toPacketOffset = (toPacket / 4092) * 4 + 48;

            int firstPacket = fromPacket + fromPacketOffset;
            int secondPacket = toPacket + toPacketOffset;

            int count = toPacket >= fromPacket ? ((secondPacket - firstPacket) / 4096) + 1 : 0;
            byte[] countBytes = BitConverter.GetBytes(count);
            SendTCP(countBytes, 4, _streamingSocket);
            byte[] currentPacketIndexBytes = new byte[4];
            byte[] currentPacketDataBytes = new byte[4092];

            int index = firstPacket;

            for (int i = 0; i < count; i++)
            {
                if (_stopSend)
                {
                    _stopSend = false;
                    SendTCP(EXIT, 4, _streamingSocket);

                    break;
                }
                SendTCP(DATA, 4, _streamingSocket);
                Buffer.BlockCopy(songData, index, currentPacketIndexBytes, 0, 4);
                index += 4;
                Buffer.BlockCopy(songData, index, currentPacketDataBytes, 0, 4092);
                index += 4092;
                SendTCP(currentPacketIndexBytes, 4, _streamingSocket);
                SendTCP(currentPacketDataBytes, 4092, _streamingSocket);
            }
        }

        /// <summary>
        /// This method will send the entire wave file of the speciifed songId to the client. It
        /// can be interrupted if _stopSend is set to false.
        /// </summary>
        /// <param name="songId"></param>
        private void SendSongData(int songId)
        {
            string sqlGetSong = @$"SELECT songFileName FROM songs WHERE songId = '{songId}'";
            SQLiteCommand command = new SQLiteCommand(sqlGetSong, _dbConnection);

            object result = command.ExecuteScalar();

            if (result != null)
            {
                string fileName = (string)result;
                byte[] songData = File.ReadAllBytes(fileName);
                byte[] header = new byte[44];
                Buffer.BlockCopy(songData, 0, header, 0, 44);
                _lastSongData = songData;
                SendTCP(MOD1, 4, _streamingSocket);
                SendTCP(header, 44, _streamingSocket);
                byte[] packetCountBytes = new byte[4];
                Buffer.BlockCopy(songData, 44, packetCountBytes, 0, 4);
                SendTCP(packetCountBytes, 4, _streamingSocket);
                int packetCount = BitConverter.ToInt32(packetCountBytes);
                int index = 48;
                byte[] currentPacketIndexBytes = new byte[4];
                byte[] currentPacketDataBytes = new byte[4092];

                for (int i = 0; i < packetCount; i++)
                {
                    if (_stopSend)
                    {
                        _stopSend = false;
                        SendTCP(EXIT, 4, _streamingSocket);
                        break;
                    }

                    SendTCP(DATA, 4, _streamingSocket);
                    Buffer.BlockCopy(songData, index, currentPacketIndexBytes, 0, 4);
                    index += 4;
                    Buffer.BlockCopy(songData, index, currentPacketDataBytes, 0, 4092);
                    index += 4092;
                    SendTCP(currentPacketIndexBytes, 4, _streamingSocket);
                    SendTCP(currentPacketDataBytes, 4092, _streamingSocket);
                }
            }
            GC.Collect();
        }

        /// <summary>
        /// This method must be run in a separate thread. It waits for the client to send the streaming mode.
        /// </summary>
        private void StreamingLoop()
        {
            try
            {
                while (true)
                {
                    byte[] mode = ReceiveTCP(4, _streamingSocket);
                    if (Enumerable.SequenceEqual(mode, MOD1))
                    {
                        byte[] songIdBytes = ReceiveTCP(4, _streamingSocket);
                        int songId = BitConverter.ToInt32(songIdBytes);
                        SendSongData(songId);
                    }
                    else if (Enumerable.SequenceEqual(mode, MOD2))
                    {
                        byte[] fromPacketBytes = ReceiveTCP(4, _streamingSocket);
                        byte[] toPacketBytes = ReceiveTCP(4, _streamingSocket);

                        int startIndex = BitConverter.ToInt32(fromPacketBytes);
                        int endIndex = BitConverter.ToInt32(toPacketBytes);
                        SendSongData(startIndex, endIndex);
                    }
                }
            }
            catch (Exception)
            {
                _streamingLoopEnded = true;
                TerminateSelf();
            }
        }

        /// <summary>
        /// If the client disconnects, ask the controller to terminate this clientHandler.
        /// </summary>
        private void TerminateSelf()
        {
            lock (_lock)
            {
                if (_streamingLoopEnded && _communicationLoopEnded)
                {
                    new ServerEvent(EventType.InternalRequest, true, InternalRequestType.RemoveClientHandler, this);
                }
            }
        }

        /// <summary>
        /// This method must be run in a separate thread. Here the clientHandler will handle the communication 
        /// with the client.
        /// </summary>
        private void CommuncationLoop()
        {
            try
            {
                while (true)
                {
                    byte[] lengthBytes = ReceiveTCP(4, _communcationSocket);
                    int length = BitConverter.ToInt32(lengthBytes);

                    byte[] requestBytes = ReceiveTCP(length, _communcationSocket);
                    string request = Encoding.UTF8.GetString(requestBytes);

                    ExecuteCommunicationRequest(request);
                }
            }
            catch (Exception)
            {
                _communicationLoopEnded = true;
                TerminateSelf();
            }
        }


        private void ExecuteCommunicationRequest(string request)
        {
            string[] translate = request.Split('@');

            if (translate[0] == "TERMINATE_SONG_DATA_RECEIVE")
            {
                TerminateSongDataReceive();
            }

            else if (translate[0] == "SEARCH") 
            {
                SearchForSong(translate[1]);
            }
        }

        /// <summary>
        /// Takes a pattern and searches the database for song names or artist names that match the pattern.
        /// If it finds songs that match the pattern, it will create Song object and serialize them into bytes.
        /// It will finally send the songs to the client. These songs do not contain song wave data.
        /// </summary>
        /// <param name="searchString"></param>
        private void SearchForSong(string searchString)
        {
            if (searchString != "")
            {
                string sqlSearchSong = $@"SELECT songId, songName, artistName, songNameSerialized,
                                        artistNameSerialized, duration, imageFileName 
                                        FROM songs
                                        WHERE songNameSerialized LIKE '%{searchString}%' or artistNameSerialized LIKE '%{searchString}%'";

                SQLiteCommand command = new SQLiteCommand(sqlSearchSong, _dbConnection);


                List<Song> results = new List<Song>();

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        byte[] imageBytes = File.ReadAllBytes(reader.GetString(6));
                        results.Add(new Song((int)reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetDouble(5), imageBytes));
                    }
                }

                int numberOfSongs = results.Count;
                byte[] numberOfSongsBytes = BitConverter.GetBytes(numberOfSongs);
                SendTCP(numberOfSongsBytes, 4, _communcationSocket);

                foreach (Song song in results)
                {
                    byte[] serializedSong = song.GetSerialized();
                    List<byte[]> packets = GetPackets(serializedSong, 1024);

                    int packetCount = packets.Count;
                    byte[] packetCountBytes = BitConverter.GetBytes(packetCount);
                    SendTCP(packetCountBytes, 4, _communcationSocket);


                    byte[] lastPacket = packets.Last();
                    int lastPacketLength = lastPacket.Length;
                    byte[] lastPacketLengthBytes = BitConverter.GetBytes(lastPacketLength);
                    SendTCP(lastPacketLengthBytes, 4, _communcationSocket);
                    SendTCP(lastPacket, lastPacketLength, _communcationSocket);

                    for (int i = 0; i < packets.Count - 1; i++)
                    {
                        SendTCP(packets[i], 1024, _communcationSocket);
                    }
                }
            }
            else
            {
                int numberOfSongs = 0;
                byte[] numberOfSongsBytes = BitConverter.GetBytes(numberOfSongs);
                SendTCP(numberOfSongsBytes, 4, _communcationSocket);
            }
        }

        /// <summary>
        /// This method will split the serializedSong into packets of packetSize.
        /// </summary>
        /// <param name="serializedSong"></param>
        /// <param name="packetSize"></param>
        /// <returns></returns>
        private List<byte[]> GetPackets(byte[] serializedSong, int packetSize)
        {
            int packetCount = serializedSong.Length / packetSize; // Possibly without last packet
            List<byte[]> packets = new List<byte[]>();

            int index = 0;
            byte[] currentPacket;

            for (int i = 0; i < packetCount; i++)
            {
                currentPacket = new byte[packetSize];
                Array.Copy(serializedSong, index, currentPacket, 0, packetSize);
                packets.Add(currentPacket);
                index += packetSize;
            }

            int lastPacketSize = serializedSong.Length - index;

            if (lastPacketSize > 0)
            {
                byte[] lastPacket = new byte[lastPacketSize];
                Array.Copy(serializedSong, index, lastPacket, 0, lastPacketSize);
                packets.Add(lastPacket);
            }

            return packets;
        }

        private void TerminateSongDataReceive()
        {
            _stopSend = true;
        }

        private void Run()
        {
            _streamingThread.Start();
            _communicationThread.Start();
        }

        /// <summary>
        /// Makes sure ReceiveTCP() receives exactly size amount of bytes.
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

        /// <summary>
        /// Makes sure SendTCP() sends exactly size abount of data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <param name="socket"></param>
        /// <exception cref="SocketException"></exception>
        private void SendTCP(byte[] data, int size, Socket socket)
        {
            int totalSent = 0;
            int x;
            while (totalSent < size)
            {
                byte[] buffer = new byte[size - totalSent];
                Buffer.BlockCopy(data, totalSent, buffer, 0, size - totalSent);
                x = socket.Send(buffer);
                if (x == 0)
                {
                    throw new SocketException();
                }
                totalSent += x;
            }
        }
    }
}
