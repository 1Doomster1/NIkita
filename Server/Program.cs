// Server.cs - обновленная версия
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleChatServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Simple Chat Server ===");
            Console.WriteLine("Listening on port 8888...");

            var server = new SimpleServer();
            await server.StartAsync();
        }
    }

    public class SimpleServer
    {
        private TcpListener _listener;
        private List<Client> _clients = new List<Client>();
        private Dictionary<string, Client> _connectedUsers = new Dictionary<string, Client>();

        public async Task StartAsync(int port = 8888)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                var clientHandler = new Client(client, this);
                _clients.Add(clientHandler);
                _ = Task.Run(() => clientHandler.HandleAsync());
            }
        }

        public void Broadcast(string message, Client exclude = null)
        {
            foreach (var client in _clients)
            {
                if (client != exclude && client.IsConnected)
                {
                    _ = client.SendAsync(message);
                }
            }
        }

        public void BroadcastToChat(string chatId, string message, Client exclude = null)
        {
            // В реальном приложении здесь была бы логика отправки в конкретный чат
            // Сейчас просто отправляем всем
            Broadcast(message, exclude);
        }

        public void UserConnected(string username, Client client)
        {
            _connectedUsers[username] = client;

            // Уведомляем всех о новом пользователе
            var notification = new NetworkMessage
            {
                Type = "user_connected",
                Content = username,
                Timestamp = DateTime.Now
            };

            Broadcast(JsonSerializer.Serialize(notification), client);
        }

        public void UserDisconnected(string username)
        {
            _connectedUsers.Remove(username);

            // Уведомляем всех об отключении
            var notification = new NetworkMessage
            {
                Type = "user_disconnected",
                Content = username,
                Timestamp = DateTime.Now
            };

            Broadcast(JsonSerializer.Serialize(notification));
        }

        public void RemoveClient(Client client)
        {
            _clients.Remove(client);
            if (!string.IsNullOrEmpty(client.Username))
            {
                UserDisconnected(client.Username);
            }
        }
    }

    public class Client
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private SimpleServer _server;
        public string Username { get; set; }
        public bool IsConnected => _tcpClient?.Connected == true;

        public Client(TcpClient tcpClient, SimpleServer server)
        {
            _tcpClient = tcpClient;
            _server = server;
            _stream = tcpClient.GetStream();
        }

        public async Task HandleAsync()
        {
            var buffer = new byte[4096];

            try
            {
                while (IsConnected)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var messages = json.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var message in messages)
                    {
                        await ProcessMessageAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task ProcessMessageAsync(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<NetworkMessage>(json);

                if (message == null) return;

                switch (message.Type)
                {
                    case "login":
                        Username = message.Content;
                        Console.WriteLine($"User connected: {Username}");
                        await SendAsync(JsonSerializer.Serialize(new NetworkMessage
                        {
                            Type = "login_success",
                            Content = Username,
                            Timestamp = DateTime.Now
                        }));
                        _server.UserConnected(Username, this);
                        break;

                    case "message":
                        Console.WriteLine($"{Username} to {message.ChatId}: {message.Content}");
                        _server.BroadcastToChat(message.ChatId, JsonSerializer.Serialize(new NetworkMessage
                        {
                            Type = "message",
                            SenderName = Username,
                            Content = message.Content,
                            ChatId = message.ChatId,
                            Timestamp = DateTime.Now
                        }), this);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        public async Task SendAsync(string message)
        {
            if (IsConnected)
            {
                var data = Encoding.UTF8.GetBytes(message + "\n");
                await _stream.WriteAsync(data, 0, data.Length);
            }
        }

        public void Disconnect()
        {
            try
            {
                Console.WriteLine($"User disconnected: {Username}");
                _server.RemoveClient(this);
                _stream?.Close();
                _tcpClient?.Close();
            }
            catch { }
        }
    }

    public class NetworkMessage
    {
        public string Type { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string ChatId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}