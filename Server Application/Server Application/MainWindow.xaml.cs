using Microsoft.Win32;
using Server_Application.DynamicVisialComponents;
using Server_Application.Server;
using System.Collections.Generic;
using System.Windows;
using Path = System.IO.Path;

namespace Server_Application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ServerListener _serverListener;
        private string _songName = "";
        private string _artistName = "";
        private string _songFilePath = "";
        private string _imageFilePath = "";
        public MainWindow()
        {
            InitializeComponent();
            _serverListener = new ServerListener();
            Listen(EventType.DisplayConnectedClients, new ServerEventCallback(ExecuteDisplayConnectedClients));
        }

        private void ExecuteDisplayConnectedClients(params object[] parameters)
        {
            List<string> connectedClients = (List<string>)parameters[0];

            Dispatcher.Invoke(() =>
            {
                connectedClientsStackPanel.Children.Clear();

                foreach (var connectedClient in connectedClients)
                {
                    connectedClientsStackPanel.Children.Add(new ConnectedClientLabel(connectedClient));
                }
            });
        }

        private void Listen(EventType eventType, ServerEventCallback serverEventCallback)
        {
            _serverListener.Listen(eventType, serverEventCallback);
        }

        private void openSongFileButton_Click(object sender, RoutedEventArgs e)
        {
            _songFilePath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Wave Files (*.wav)|*.wav";
            if (openFileDialog.ShowDialog() == true)
            {
                _songFilePath = openFileDialog.FileName;
                songFileNameLabel.Content = Path.GetFileName(openFileDialog.FileName);
            }
        }

        private void openImageFileButton_Click(object sender, RoutedEventArgs e)
        {
            _imageFilePath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Wave Files (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                _imageFilePath = openFileDialog.FileName;
                imageFileNameLabel.Content = Path.GetFileName(openFileDialog.FileName);
            }
        }

        private void addToDatabaseButton_Click(object sender, RoutedEventArgs e)
        {

            _songName = songNameTextBox.Text;
            _artistName = artistNameTextBox.Text;

            string errorMessage = "";
            bool error = false;
            if (_songName == "")
            {
                errorMessage += " [Song Name]";
                error = true;
            }
            if (_artistName == "")
            {
                errorMessage += " [Artist Name]";
                error = true;
            }
            if (_songFilePath == "")
            {
                errorMessage += " [Song File]";
                error = true;
            }
            if (_imageFilePath == "")
            {
                errorMessage += " [Image File]";
                error = true;
            }

            if (error)
            {
                MessageBox.Show($"You must provide {errorMessage}");
            }
            else
            {
                new ServerEvent(EventType.InternalRequest, true, InternalRequestType.AddToDatabase, _songName, _artistName, _songFilePath, _imageFilePath);
            }
        }
    }
}
