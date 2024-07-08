using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace BLUFF_CITY
{
    public static class Client
    {
        private static TcpClient client;
        private static NetworkStream stream;

        public static void ConnectToServer()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 13000);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        public static void SendLoginInfo(string id, string nickname)
        {
            try
            {
                if (client == null || !client.Connected)
                {
                    ConnectToServer();
                }

                string message = $"login:{id}:{nickname}";
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        public static void JoinGame(string id, string nickname, string gameRoom)
        {
            try
            {
                if (client == null || !client.Connected)
                {
                    ConnectToServer();
                }

                string message = $"join:{id}:{nickname}:{gameRoom}";
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

            public static void SendMessage(string gameRoom, string message)
        {
            try
            {
                string fullMessage = $"{gameRoom}:{message}";
                byte[] data = Encoding.ASCII.GetBytes(fullMessage);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        public static void CloseConnection()
        {
            try
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}
