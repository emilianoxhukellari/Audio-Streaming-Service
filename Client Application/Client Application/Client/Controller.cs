using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Client_Application.Client
{

    public sealed class Controller
    {
        // ATTRIBUTES
        private MediaPlayer _mediaPlayer;
        private readonly ClientListener _clientListener;
        private ProgressBarState _progressBarState;
        private readonly string _playlistsRelateivePath;
        private string _currentPlaylist;
        private readonly Queue<NetworkRequest> _networkRequestQueue;
        private readonly Queue<InternalRequest> _internalRequestQueue;
        // ATTRIBUTES

        // NETWORKING
        private DualSocket _dualSocket;
        private readonly int _portCommunication;
        private readonly int _portStreaming;
        private readonly string _host;
        private readonly string _clientId;
        private readonly IPAddress _IP;
        private readonly IPEndPoint _controllerIPE;
        private readonly IPEndPoint _mediaPlayerIPE;
        // NETWORKING

        // THREADS AND TASKS
        private readonly Thread _internalRequestThread;
        private readonly Thread _communicationThread;
        private readonly Task _progressTask;
        // THREADS AND TASKS

        // THREADING RESET EVENTS
        private readonly AutoResetEvent _newNetworkRequestFlag;
        private readonly AutoResetEvent _newInternalRequestFlag;
        // THREADING RESET EVENTS

        public Controller()
        {
            _portCommunication = Config.Config.GetPortCommunication();
            _portStreaming = Config.Config.GetPortStreaming();
            _host = Config.Config.GetHost();
            _clientId = Config.Config.GetClientId();
            _IP = IPAddress.Parse(_host);
            _controllerIPE = new IPEndPoint(_IP, _portCommunication);
            _mediaPlayerIPE = new IPEndPoint(_IP, _portStreaming);
            _dualSocket = new DualSocket(_controllerIPE, _mediaPlayerIPE, _clientId);
            _playlistsRelateivePath = Config.Config.GetPlaylistsRelativePath();
            _currentPlaylist = "";
            _internalRequestThread = new Thread(InternalRequestLoop);
            _internalRequestThread.IsBackground = true;
            _communicationThread = new Thread(CommunicationLoop);
            _communicationThread.IsBackground = true;
            _progressTask = new Task(ProgressLoop);
            _newNetworkRequestFlag = new AutoResetEvent(false);
            _newInternalRequestFlag = new AutoResetEvent(false);
            _clientListener = new ClientListener();
            _networkRequestQueue = new Queue<NetworkRequest>();
            _internalRequestQueue = new Queue<InternalRequest>();
            _progressBarState = ProgressBarState.Free;
            _mediaPlayer = new MediaPlayer(_dualSocket,
                new CallbackTerminateSongDataReceive(TerminateSongDataReceiveRequest),
                new CallbackSendCurrentSongInfo(UpdateCurrentSongInfo),
                new CallbackSendQueueInfo(DisplayQueue),
                new CallbackUpdateRepeatState(UpdateRepeatState));

            // Listen for events from Windows
            Listen(EventType.NetworkRequest, new ClientEventCallback(AddNetworkRequest));
            Listen(EventType.InternalRequest, new ClientEventCallback(AddInternalRequest));
            Listen(EventType.UpdateProgressBarState, new ClientEventCallback(SetProgressBarState));
            Listen(EventType.MoveSongUpQueue, new ClientEventCallback(ExecuteMoveSongUpQueue));
            Listen(EventType.MoveSongDownQueue, new ClientEventCallback(ExecuteMoveSongDownQueue));
            Listen(EventType.RemoveSongQueue, new ClientEventCallback(ExecuteRemoveSongQueue));
            Listen(EventType.DeleteQueue, new ClientEventCallback(ExecuteDeleteQueue));
            Listen(EventType.ChangeVolume, new ClientEventCallback(ExecuteChangeVolume));
            Listen(EventType.CreateNewPlaylist, new ClientEventCallback(ExecuteCreateNewPlaylist));
            Listen(EventType.RequestDisplayPlaylistLinks, new ClientEventCallback(ExecuteRequestDisplayPlaylistLinks));
            Listen(EventType.UpdatePlaylist, new ClientEventCallback(ExecuteUpdatePlaylist));
            Listen(EventType.AddSongToPlaylist, new ClientEventCallback(ExecuteAddSongToPlaylist));
            Listen(EventType.RemoveSongFromPlaylist, new ClientEventCallback(ExecuteRemoveSongFromPlaylist));
            Listen(EventType.PlayCurrentPlaylist, new ClientEventCallback(ExecutePlayCurrentPlaylist));
            Listen(EventType.RenamePlaylist, new ClientEventCallback(ExecuteRenamePlaylist));
            Listen(EventType.AddPlaylistToQueue, new ClientEventCallback(ExecuteAddPlaylistToQueue));
            Listen(EventType.DeletePlaylist, new ClientEventCallback(ExecuteDeletePlaylist));
            Listen(EventType.SearchPlaylist, new ClientEventCallback(ExecuteSearchPlaylist));
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to move the song at index up the queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteMoveSongUpQueue(params object[] parameters)
        {
            int index = (int)parameters[0];
            _mediaPlayer.MoveSongUp(index);
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to move the song at index down the queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteMoveSongDownQueue(params object[] parameters)
        {
            int index = (int)parameters[0];
            _mediaPlayer.MoveSongDown(index);
        }

        /// <summary>
        /// Expects no parameters. It will ask the media player to delete the entire queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteDeleteQueue(params object[] parameters)
        {
            _mediaPlayer.DeleteQueue();
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to remove song at index from queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteRemoveSongQueue(params object[] parameters)
        {
            int index = (int)parameters[0];
            _mediaPlayer.RemoveSongFromQueue(index);
        }

        /// <summary>
        /// Expects (string)search. It will Seach for a song in the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteSearchPlaylist(params object[] parameters)
        {
            string searchString = (string)parameters[0];
            SearchPlaylistSong(searchString);
        }

        /// <summary>
        /// Expects (float)volume from 0 to 100. It will ask the media player to change the volume from 0 to 1.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteChangeVolume(params object[] parameters)
        {
            float volume100 = (float)parameters[0];
            float volume = volume100 / 100;
            _mediaPlayer.SetVolume(volume);
        }

        /// <summary>
        /// Expects no parameters. It delets the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteDeletePlaylist(params object[] parameters)
        {
            string target = @$"{_playlistsRelateivePath}{_currentPlaylist}\";
            Directory.Delete(target, true);
            DisplayPlaylistLinks(DisplayPlaylistLinksMode.Delete);
        }

        /// <summary>
        /// Expects (Song)song and (string) playlist name. It will add the specified song to the specified playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteAddSongToPlaylist(params object[] parameters)
        {
            Song song = (Song)parameters[0];
            string playlistLink = (string)parameters[1];
            AddSongToPlaylist(song, playlistLink);
        }

        /// <summary>
        /// Expects (string)new name. It renames the current playlist with the new name.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteRenamePlaylist(params object[] parameters)
        {
            string newName = (string)parameters[0];

            string source = $"{_playlistsRelateivePath}{_currentPlaylist}";
            string destination = $"{_playlistsRelateivePath}{newName}";
            if (!Directory.Exists(destination))
            {
                Directory.Move(source, destination);
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.Rename, newName);
                _currentPlaylist = newName;
            }
            else
            {
                new ClientEvent(EventType.PlaylistExists, true, newName);
            }
        }

        /// <summary>
        /// Expects (string)name. It will create an empty playlist with the name and display empty playlist window.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteCreateNewPlaylist(params object[] parameters)
        {
            string playlistName = (string)parameters[0];
            if (playlistName != "")
            {
                if (Directory.Exists($"{_playlistsRelateivePath}{playlistName}"))
                {
                    new ClientEvent(EventType.PlaylistExists, true, playlistName);
                }
                else
                {
                    Directory.CreateDirectory($"{_playlistsRelateivePath}{playlistName}");
                    _currentPlaylist = playlistName;
                    DisplayPlaylistLinks(DisplayPlaylistLinksMode.New, playlistName);
                }
            }
        }

        /// <summary>
        /// Expects no parameters. It will add the current playlist to queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteAddPlaylistToQueue(params object[] parameters)
        {
            AddPlaylistToQueue();
        }

        /// <summary>
        /// Expects no parameters. It will play the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecutePlayCurrentPlaylist(params object[] parameters)
        {
            PlayCurrentPlaylist();
        }

        /// <summary>
        /// Expects (string)playlist name. It will update that playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteUpdatePlaylist(params object[] parameters)
        {
            string playlistLink = (string)parameters[0];
            UpdatePlaylist(playlistLink);
        }

        /// <summary>
        /// Expects (Song)song and (string)playlist name. It will remove that song from that playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteRemoveSongFromPlaylist(params object[] parameters)
        {
            Song song = (Song)parameters[0];
            string playlistLink = (string)parameters[1];
            RemoveSongFromPlaylist(song, playlistLink);
        }

        /// <summary>
        /// Expects no parameters. It will request for the playlist names to be displayed.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteRequestDisplayPlaylistLinks(params object[] parameters)
        {
            new Task(() =>
            {
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.None);
            }).Start();
        }

        /// <summary>
        /// This methods takes an InternalRequest and executes it based on the type.
        /// </summary>
        /// <param name="internalRequest"></param>
        private void ExecuteInternalRequest(InternalRequest internalRequest)
        {
            if (internalRequest.Type == InternalRequestType.PlayPauseStateChange)
            {
                ExecutePlayPauseStateChange();
            }
            else if (internalRequest.Type == InternalRequestType.PlayThis)
            {
                Song song = (Song)internalRequest.Parameters[0];
                PlayThis(song);
            }
            else if (internalRequest.Type == InternalRequestType.NextSong)
            {
                NextSong();
            }
            else if (internalRequest.Type == InternalRequestType.PreviousSong)
            {
                PreviousSong();
            }
            else if (internalRequest.Type == InternalRequestType.AddSongToQueue)
            {
                Song song = (Song)internalRequest.Parameters[0];
                AddToQueue(song);
            }
            else if (internalRequest.Type == InternalRequestType.RepeatStateChange)
            {
                RepeatState repeatState = (RepeatState)internalRequest.Parameters[0];
                ExecuteRepeatStateChange(repeatState);
            }
            else if (internalRequest.Type == InternalRequestType.ShuffleStateChange)
            {
                ShuffleState shuffleState = (ShuffleState)internalRequest.Parameters[0];
                ExecuteShuffleStateChange(shuffleState);
            }
        }

        /// <summary>
        /// This method will change the play state. It will then send the new state to main window.
        /// </summary>
        private void ExecutePlayPauseStateChange()
        {
            PlaybackState playbackState = _mediaPlayer.ChangePlayState();

            switch (playbackState)
            {
                case PlaybackState.Playing:
                    SendPlayState(PlayButtonState.Play);
                    break;
                case PlaybackState.Paused:
                    SendPlayState(PlayButtonState.Pause);
                    break;
            }
        }

        /// <summary>
        /// This method will set the shuffle state of the media player.
        /// </summary>
        /// <param name="shuffleState"></param>
        private void ExecuteShuffleStateChange(ShuffleState shuffleState)
        {
            _mediaPlayer.SetShuffleState(shuffleState);
        }

        /// <summary>
        /// This method will set the repeat state of the media player.
        /// </summary>
        /// <param name="repeatState"></param>
        private void ExecuteRepeatStateChange(RepeatState repeatState)
        {
            _mediaPlayer.SetRepeatState(repeatState);
        }

        /// <summary>
        /// This method will ask the window to update the repeat state with the new one.
        /// </summary>
        /// <param name="repeatState"></param>
        private void UpdateRepeatState(RepeatState repeatState)
        {
            new ClientEvent(EventType.UpdateRepeatState, true, repeatState);
        }

        /// <summary>
        /// This method will ask the window to display the playlist names. 
        /// </summary>
        /// <param name="displayPlaylistLinksMode"></param>
        /// <param name="active"></param>
        private void DisplayPlaylistLinks(DisplayPlaylistLinksMode displayPlaylistLinksMode, string active = "")
        {
            string[] playlistLinks = GetPlaylistLinks();

            if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.None || displayPlaylistLinksMode == DisplayPlaylistLinksMode.Delete)
            {
                new ClientEvent(EventType.DisplayPlaylistLinks, true, playlistLinks, displayPlaylistLinksMode);
            }
            else if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.New || displayPlaylistLinksMode == DisplayPlaylistLinksMode.Rename)
            {
                new ClientEvent(EventType.DisplayPlaylistLinks, true, playlistLinks, displayPlaylistLinksMode, active);
            }
        }

        /// <summary>
        /// It will get all playlist songs from the current playlist.
        /// </summary>
        /// <param name="playlistLink"></param>
        /// <returns></returns>
        private List<Song> GetPlaylistSongs(string playlistLink)
        {
            List<Song> songs = new List<Song>(0);
            if (Directory.Exists(@$"{_playlistsRelateivePath}{playlistLink}\"))
            {
                foreach (string file in Directory.EnumerateFiles(@$"{_playlistsRelateivePath}{playlistLink}\"))
                {
                    songs.Add(new Song(File.ReadAllBytes(file)));
                }
            }
            return songs;
        }

        /// <summary>
        /// It will get all playlist names.
        /// </summary>
        /// <returns></returns>
        private string[] GetPlaylistLinks()
        {
            return Directory.GetDirectories(_playlistsRelateivePath).Select(d => Path.GetRelativePath(_playlistsRelateivePath, d)).ToArray();
        }

        /// <summary>
        /// This method updates the current playlist.
        /// </summary>
        /// <param name="playlistLink"></param>
        private void UpdatePlaylist(string playlistLink)
        {
            _currentPlaylist = playlistLink;
            new ClientEvent(EventType.DisplayPlaylistSongs, true, GetPlaylistSongs(_currentPlaylist), _currentPlaylist);
        }

        /// <summary>
        /// This method will remove the specified song from the playlist. It will then update the playlist.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="playlistLink"></param>
        private void RemoveSongFromPlaylist(Song song, string playlistLink)
        {
            List<Song> songs = GetPlaylistSongs(playlistLink);
            songs.RemoveAll(s => s.SongId == song.SongId);

            string fullSongPath = GetFullSongPath(song, playlistLink);
            if (File.Exists(fullSongPath))
            {
                File.Delete(fullSongPath);
            }
            UpdatePlaylist(_currentPlaylist);
        }

        private string GetFullSongPath(Song song, string playlistLink)
        {
            string fileName = $"{song.SongName} by {song.ArtistName}.bytes";
            return @$"{_playlistsRelateivePath}{playlistLink}\{fileName}";
        }

        private void AddSongToPlaylist(Song song, string playlistLink)
        {
            string fullSongPath = GetFullSongPath(song, playlistLink);

            if (File.Exists(fullSongPath))
            {
                MessageBox.Show($"{song.SongName} by {song.ArtistName} is already in {playlistLink}.", 
                    "Cannot add song to playlist", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
            else
            {
                File.WriteAllBytes(fullSongPath, song.GetSerialized());
            }
        }

        private void AddPlaylistToQueue()
        {
            List<Song> playlistSongs = GetPlaylistSongs(_currentPlaylist);
            _mediaPlayer.AddPlaylistSongsToQueue(playlistSongs);
        }


        /// <summary>
        /// It will search for the pattern in the current playlist. If the pattern matches song names or artist names,
        /// it will display those songs to the window.
        /// </summary>
        /// <param name="searchString"></param>
        private void SearchPlaylistSong(string searchString)
        {
            string serializedString = new string(searchString.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray());

            if (serializedString == "") // If empty display all songs
            {
                new ClientEvent(EventType.DisplayPlaylistSongs, true, GetPlaylistSongs(_currentPlaylist), _currentPlaylist);
                return;
            }

            List<Song> results = new List<Song>();
            List<Song> playlistSongs = GetPlaylistSongs(_currentPlaylist);

            for (int i = 0; i < playlistSongs.Count; i++)
            {
                if (Regex.Match(playlistSongs[i].SongName, serializedString, RegexOptions.IgnoreCase).Success ||
                        Regex.Match(playlistSongs[i].ArtistName, serializedString, RegexOptions.IgnoreCase).Success)
                {
                    results.Add(playlistSongs[i]);
                }
            }
            new ClientEvent(EventType.DisplayPlaylistSongs, true, results, _currentPlaylist);
        }

        /// <summary>
        /// Asks the mediaplayer to play the songs that belong to the current playlist.
        /// </summary>
        private void PlayCurrentPlaylist()
        {
            List<Song> playlistSongs = GetPlaylistSongs(_currentPlaylist);
            if(playlistSongs.Any())
            {
                _mediaPlayer.PlayPlaylistSongs(playlistSongs);
                SendPlayState(PlayButtonState.Play);
            }
        }

        /// <summary>
        /// Callback method for mediaplayer.
        /// </summary>
        /// <param name="songs"></param>
        private void DisplayQueue(List<(Song, int)> songs)
        {
            new ClientEvent(EventType.DisplayQueue, true, songs);
        }

        /// <summary>
        /// Updates Progress Bar every 500 ms.
        /// </summary>
        private void ProgressLoop()
        {
            while (true)
            {
                Thread.Sleep(500);
                if (_progressBarState == ProgressBarState.Free)
                {
                    (double Progress, string CurrentTime) progress = _mediaPlayer.GetCurrentSongProgress();
                    new ClientEvent(EventType.UpdateProgress, true, progress.Progress, progress.CurrentTime);
                }
            }
        }

        private void SetProgressBarState(params object[] parameters)
        {
            ProgressBarState state = (ProgressBarState)parameters[0];
            if (state == ProgressBarState.Free)
            {
                double progress = (double)parameters[1];
                ExecuteChangeProgress(progress);
            }
            _progressBarState = state;
        }

        private void ExecuteChangeProgress(double progress_0To100)
        {
            if (progress_0To100 <= 100 && progress_0To100 >= 0)
            {
                double audioStreamPosition = progress_0To100 / 100;
                _mediaPlayer.SetPosition(audioStreamPosition);
            }
        }

        private void UpdateCurrentSongInfo(Song song)
        {
            new ClientEvent(EventType.DisplayCurrentSong, true, song.SongName, song.ArtistName, song.DurationString, song.ImageBinary);
        }

        private void TerminateSongDataReceiveRequest()
        {
            AddNetworkRequest(NetworkRequestType.TerminateSongDataReceive);
        }

        private void AddNetworkRequest(params object[] parameters)
        {
            _networkRequestQueue.Enqueue(new NetworkRequest(parameters));
            _newNetworkRequestFlag.Set();
        }

        /// <summary>
        /// Adds the specified song to mediaplayer queue.
        /// </summary>
        /// <param name="song"></param>
        private void AddToQueue(Song song)
        {
            _mediaPlayer.AddToQueue(song);
        }

        private void SendPlayState(PlayButtonState playButtonState)
        {
            new ClientEvent(EventType.ChangePlayState, true, playButtonState);
        }

        private void ExecuteNetworkRequest(NetworkRequest networkRequest)
        {
            if (networkRequest.Type == NetworkRequestType.SearchSongOrArtist)
            {
                ExecuteSearchRequest(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.TerminateSongDataReceive)
            {
                ExecuteTerminateSongDataReceiveRequest();
            }
        }

        /// <summary>
        /// Ask the server to stop sound data.
        /// </summary>
        private void ExecuteTerminateSongDataReceiveRequest()
        {
            string request = "TERMINATE_SONG_DATA_RECEIVE@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);

            int length = requestBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(length);

            try
            {
                ClientTCP.SendTCP(lengthBytes, 4, _dualSocket.ControllerSocket);
                ClientTCP.SendTCP(requestBytes, length, _dualSocket.ControllerSocket);
            }
            catch (SocketException)
            {
                _dualSocket.Reconnect();
            }
        }

        private void AddInternalRequest(params object[] parameters)
        {
            _internalRequestQueue.Enqueue(new InternalRequest(parameters));
            _newInternalRequestFlag.Set();
        }

        /// <summary>
        /// This method should be run in a separate Thread/Task. 
        /// It will execute internal requests. These internal requests are related to playback.
        /// </summary>
        private void InternalRequestLoop()
        {
            while (true)
            {
                if (_internalRequestQueue.Count == 0)
                {
                    _newInternalRequestFlag.WaitOne();
                }

                InternalRequest? internalRequest;

                if (_internalRequestQueue.TryDequeue(out internalRequest))
                {
                    ExecuteInternalRequest(internalRequest);
                }
            }
        }

        /// <summary>
        /// This method should be run in a separate Thread/Task.
        /// It will execute network requests. Searching for song or artist, and terminate sound data command are network requests.
        /// </summary>
        private void CommunicationLoop()
        {
            while (true)
            {
                if (_networkRequestQueue.Count == 0)
                {
                    _newNetworkRequestFlag.WaitOne();
                }

                NetworkRequest? networkRequest;

                if (_networkRequestQueue.TryDequeue(out networkRequest))
                {
                    ExecuteNetworkRequest(networkRequest);
                }
            }
        }

        /// <summary>
        /// Expects (string)inpput. It will take this input and remove white spaces.
        /// Thereafter, it will send this input to the server and it will wait for results.
        /// All the results are saved and sent to the Search Window.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteSearchRequest(object[] parameters)
        {
            string searchString = (string)parameters[0];
            string searchSongSerialized = string.Concat(searchString.Where(c => !char.IsWhiteSpace(c)));
            List<Song> foundSongs = new List<Song>(0);

            if (searchSongSerialized == "")
            {
                new ClientEvent(EventType.DisplaySongs, true, foundSongs); // Display empty
            }

            else
            {
                try
                {
                    string request = $"SEARCH@{searchSongSerialized}";
                    byte[] requestBytes = Encoding.UTF8.GetBytes(request);

                    int length = requestBytes.Length;
                    byte[] lengthBytes = BitConverter.GetBytes(length);

                    ClientTCP.SendTCP(lengthBytes, 4, _dualSocket.ControllerSocket);
                    ClientTCP.SendTCP(requestBytes, length, _dualSocket.ControllerSocket);

                    byte[] numberOfSongsBytes = ClientTCP.ReceiveTCP(4, _dualSocket.ControllerSocket);
                    int numberOfSongs = BitConverter.ToInt32(numberOfSongsBytes);

                    for (int i = 0; i < numberOfSongs; i++)
                    {

                        byte[] packetsCountBytes = ClientTCP.ReceiveTCP(4, _dualSocket.ControllerSocket);
                        int packetsCount = BitConverter.ToInt32(packetsCountBytes);


                        byte[] lastPacketLengthBytes = ClientTCP.ReceiveTCP(4, _dualSocket.ControllerSocket); // This is the last packet which is likely to be less than 1024 bytes
                        int lastPacketLength = BitConverter.ToInt32(lastPacketLengthBytes);

                        byte[] songBytes = new byte[packetsCount * 1024 + lastPacketLength]; // Full packets + last packet
                        byte[] lastPacket = ClientTCP.ReceiveTCP(lastPacketLength, _dualSocket.ControllerSocket);

                        Buffer.BlockCopy(lastPacket, 0, songBytes, songBytes.Length - lastPacketLength, lastPacketLength); // Add last packet at the end 

                        int index = 0;

                        for (int j = 0; j < packetsCount - 1; j++)
                        {
                            Buffer.BlockCopy(ClientTCP.ReceiveTCP(1024, _dualSocket.ControllerSocket), 0, songBytes, index, 1024);
                            index += 1024;
                        }
                        foundSongs.Add(new Song(songBytes));
                    }
                }
                catch (SocketException)
                {
                    _dualSocket.Reconnect();
                }
                finally
                {
                    new ClientEvent(EventType.DisplaySongs, true, foundSongs);
                }
            }
        }

        /// <summary>
        /// Asks media player to play the specified song.
        /// </summary>
        /// <param name="song"></param>
        public void PlayThis(Song song)
        {
            SendPlayState(PlayButtonState.Play);
            _mediaPlayer.PlayThis(song);
        }

        /// <summary>
        /// Asks media player to play next song.
        /// </summary>
        public void NextSong()
        {
            SendPlayState(PlayButtonState.Play);
            _mediaPlayer.NextSong();
        }

        /// <summary>
        /// Asks media player to play previous song.
        /// </summary>
        public void PreviousSong()
        {
            SendPlayState(PlayButtonState.Play);
            _mediaPlayer.PreviousSong();
        }

        /// <summary>
        /// Subscribes for an event. The callback will be called when this event happens.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="callback"></param>
        private void Listen(EventType eventType, ClientEventCallback callback)
        {
            _clientListener.Listen(eventType, callback);
        }

        /// <summary>
        /// Call this method to start the controller.
        /// </summary>
        public void Run()
        {
            _mediaPlayer.Run();
            _communicationThread.Start();
            _internalRequestThread.Start();
            _progressTask.Start();
        }
    }
}
