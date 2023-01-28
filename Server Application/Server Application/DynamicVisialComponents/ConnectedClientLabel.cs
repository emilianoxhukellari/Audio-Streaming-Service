using System.Windows.Controls;
using System.Windows.Media;

namespace Server_Application.DynamicVisialComponents
{
    public class ConnectedClientLabel : Label
    {
        public ConnectedClientLabel(string clientId) : base()
        {
            Content = clientId;
            Foreground = new SolidColorBrush(Colors.White);
            FontSize = 14;
            Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
        }
    }
}
