using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace server
{
    class Program
    {
        static Dictionary<string, List<TcpClient>> gameRooms = new Dictionary<string, List<TcpClient>>();
        static List<string> playerInfo = new List<string>();  // 로그인한 플레이어 정보 저장

        static void Main(string[] args)
        {
            TcpListener server = null;
            try
            {

                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                server = new TcpListener(localAddr, port);
                server.Start();

                Console.WriteLine("Server started...");


                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
            finally
            {
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private static void BroadcastMessage(string gameRoom, string message, TcpClient excludeClient)
        {
            lock (gameRooms)
            {
                if (gameRooms.ContainsKey(gameRoom))
                {
                    byte[] data = Encoding.ASCII.GetBytes($"{gameRoom}:{message}");
                    foreach (var client in gameRooms[gameRoom])
                    {
                        if (client != excludeClient)
                        {
                            NetworkStream stream = client.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[256];
            string gameRoom = null;

            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string initialMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                string[] messageParts = initialMessage.Split(':');
                string action = messageParts[0];
                string playerID = messageParts[1];
                string playerNick = messageParts[2];
                gameRoom = messageParts.Length > 3 ? messageParts[3] : null;

                if (action == "login")
                {
                    lock (playerInfo)
                    {
                        playerInfo.Add($"{playerID}:{playerNick}");
                    }
                    Console.WriteLine($"플레이어 로그인 - ID: {playerID}, 닉네임: {playerNick}");
                }
                else if (action == "join")
                {
                    Console.WriteLine($"플레이어 게임 입장 - ID: {playerID}, 닉네임: {playerNick}, 게임: {gameRoom}");

                    lock (gameRooms)
                    {
                        if (!gameRooms.ContainsKey(gameRoom))
                        {
                            gameRooms[gameRoom] = new List<TcpClient>();
                        }

                        if (gameRooms[gameRoom].Count < 8)
                        {
                            gameRooms[gameRoom].Add(client);
                            Console.WriteLine($"클라이언트가 방에 입장했습니다: {gameRoom}");

                            BroadcastMessage(gameRoom, $"{playerNick}님이 게임에 참가했습니다!", client);

                            SendPlayerInfo(gameRoom);
                        }
                        else
                        {
                            byte[] msg = Encoding.ASCII.GetBytes("방이 가득 찼습니다");
                            stream.Write(msg, 0, msg.Length);
                            client.Close();
                            return;
                        }
                    }
                }

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"수신: {message}");

                    string[] parts = message.Split(new[] { ':' }, 2);
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("잘못된 메시지 형식");
                        continue;
                    }

                    string command = parts[0];
                    string content = parts[1];

                    if (command == "chat")
                    {
                        string[] chatParts = content.Split(new[] { ':' }, 2);
                        if (chatParts.Length < 2)
                        {
                            Console.WriteLine("잘못된 채팅 메시지 형식");
                            continue;
                        }

                        string nickname = chatParts[0];
                        string chatMessage = chatParts[1];

                        SaveMessageToDatabase(nickname, chatMessage);

                        // 모든 저장된 메시지를 클라이언트에 전송
                        SendAllMessagesToClient(client, gameRoom);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (gameRoom != null)
                {
                    lock (gameRooms)
                    {
                        gameRooms[gameRoom].Remove(client);
                        BroadcastMessage(gameRoom, $"{gameRoom}: 플레이어가 게임에서 나갔습니다!", client);
                    }
                }
                client.Close();
            }
        }
        private static void SendAllMessagesToClient(TcpClient client, string gameRoom)
        {
            string connectionString = "Server=localhost; Database=bluff_city; Uid=bluff_city; Pwd=bluff_city;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT nickname, mafia_chat FROM mafia_chats ORDER BY timestamp";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        StringBuilder allMessages = new StringBuilder();
                        while (reader.Read())
                        {
                            string nickname = reader.GetString("nickname");
                            string message = reader.GetString("mafia_chat");
                            allMessages.AppendLine($"\n[{nickname}] {message}");
                        }
                        byte[] data = Encoding.ASCII.GetBytes(allMessages.ToString());
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Database Error: " + ex.Message);
                }
            }
        }

        private static void SendPlayerInfo(string gameRoom)
        {
            string playerListMessage = "player_info:";
            lock (playerInfo)
            {
                foreach (var p in playerInfo)
                {
                    playerListMessage += $"{p},";
                }
            }
            playerListMessage = playerListMessage.TrimEnd(',');

            lock (gameRooms)
            {
                foreach (var client in gameRooms[gameRoom])
                {
                    NetworkStream stream = client.GetStream();
                    byte[] data = Encoding.ASCII.GetBytes(playerListMessage);
                    stream.Write(data, 0, data.Length);
                }
            }
        }
        private static void SaveMessageToDatabase(string nickname, string message)
        {
            string connectionString = "Server=localhost; Database=bluff_city; Uid=bluff_city; Pwd=bluff_city;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO mafia_chats (nickname, mafia_chat) VALUES (@nickname, @message)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nickname", nickname);
                    cmd.Parameters.AddWithValue("@message", message);
                    cmd.ExecuteNonQuery();

                    // 데이터베이스에 메시지가 저장된 후에 로드하여 클라이언트에 전송
                    LoadMessagesFromDatabase();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Database Error: " + ex.Message);
                }
            }
        }

        private static void LoadMessagesFromDatabase()
        {
            string connectionString = "Server=localhost; Database=bluff_city; Uid=bluff_city; Pwd=bluff_city;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT nickname, mafia_chat FROM mafia_chats ORDER BY timestamp";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string nickname = reader.GetString("nickname");
                            string message = reader.GetString("mafia_chat");
                            BroadcastMessage("mafia_game", $"{nickname}: {message}", null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Database Error: " + ex.Message);
                }
            }
        }

    }
}
