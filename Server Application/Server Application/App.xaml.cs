using Server_Application.Server;
using System.Windows;

namespace Server_Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal Controller controller = new Controller();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            controller.Run();
        }
    }
}
