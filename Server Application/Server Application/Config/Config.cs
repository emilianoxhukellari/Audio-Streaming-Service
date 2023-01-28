using System;
using System.Configuration;

namespace Server_Application.Config
{
    public static class Config
    {
        public static string GetHost()
        {
            var configHost = ConfigurationManager.AppSettings["Host"];
            return configHost != null ? configHost : string.Empty;
        }

        public static int GetPortCommunication()
        {
            var configPortCommunication = ConfigurationManager.AppSettings["PortCommunication"];
            return configPortCommunication != null ? Int32.Parse(configPortCommunication) : 8181;
        }

        public static int GetPortStreaming()
        {
            var configPortStreaming = ConfigurationManager.AppSettings["PortStreaming"];
            return configPortStreaming != null ? Int32.Parse(configPortStreaming) : 8080;
        }

        public static string GetDatabaseRelativePath()
        {
            var configDatabaseRelativePath = ConfigurationManager.AppSettings["DatabaseRelativePath"];
            return configDatabaseRelativePath != null ? configDatabaseRelativePath : string.Empty;
        }

        public static string GetAudioFilesRelativePath()
        {
            var configAudioFilesRelativePath = ConfigurationManager.AppSettings["AudioFilesRelativePath"];
            return configAudioFilesRelativePath != null ? configAudioFilesRelativePath : string.Empty;
        }

        public static string GetImageFilesRelativePath()
        {
            var configImageFilesRelativePath = ConfigurationManager.AppSettings["ImageFilesRelativePath"];
            return configImageFilesRelativePath != null ? configImageFilesRelativePath : string.Empty;
        }
    }
}
