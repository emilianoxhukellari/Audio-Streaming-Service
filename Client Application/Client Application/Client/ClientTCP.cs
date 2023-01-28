using System;
using System.Net.Sockets;

namespace Client_Application.Client
{
    /// <summary>
    /// Call ReceiveTCP() to receive exactly size amount of bytes. 
    /// Call SendTCP() to receive exactly size amount of bytes.
    /// </summary>
    public static class ClientTCP
    {
        public static byte[] ReceiveTCP(int size, Socket socket)
        {
            byte[] packet = new byte[size];
            int bytesReceived = 0;
            int x;
            while (bytesReceived < size)
            {
                byte[] buffer = new byte[size - bytesReceived];
                x = socket.Receive(buffer);
                Buffer.BlockCopy(buffer, 0, packet, bytesReceived, x);
                bytesReceived += x;
            }
            return packet;
        }
        public static void SendTCP(byte[] data, int size, Socket socket)
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
