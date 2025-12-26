using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace NikitaMicrosoft.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Nikita Messenger Server";

            var server = new ChatServer();
            await server.StartAsync();
        }
    }

    public class ChatServer
    {
        private TcpListener _listener;
        private readonly List<ClientHandler> _clients = new List<ClientHandler>();
        private readonly Dictionary<string, ChatRoom> _chatRooms = new Dictionary<string, ChatRoom>();
        private readonly Dictionary<string, User> _users = new Dictionary<string, User>();
        private bool _isRunning = true;

        public async Task StartAsync(int port = 8888)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();

                Console.WriteLine($"Server started on port {port}");
                Console.WriteLine($"Listening for connections...");
                Console.WriteLine();

                CreateChatRoom("general", "General Chat");

                while (_isRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    var handler = new ClientHandler(client, this);
                    _clients.Add(handler);
                    _ = Task.Run(() => handler.HandleClientAsync());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Server error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();

            // Отключаем всех клиентов
            foreach (var client in _clients.ToArray())
            {
                client.Disconnect();
            }

            Console.WriteLine("🛑 Server stopped");
        }

        public void RegisterUser(string userId, string username, ClientHandler handler)
        {
            var user = new User
            {
                Id = userId,
                Username = username,
                IsOnline = true,
                LastSeen = DateTime.Now,
                Handler = handler
            };

            _users[userId] = user;
            Console.WriteLine($"👤 User connected: {username} ({userId})");

            // Уведомляем всех о новом пользователе
            BroadcastSystemMessage($"{username} присоединился к чату");
        }

        public void UnregisterUser(string userId)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.Now;
                Console.WriteLine($"👤 User disconnected: {user.Username} ({userId})");

                // Уведомляем всех об отключении
                BroadcastSystemMessage($"{user.Username} покинул чат");
            }
        }

        public User GetUser(string userId)
        {
            _users.TryGetValue(userId, out var user);
            return user;
        }

        public List<User> GetOnlineUsers()
        {
            var onlineUsers = new List<User>();
            foreach (var user in _users.Values)
            {
                if (user.IsOnline)
                {
                    onlineUsers.Add(user);
                }
            }
            return onlineUsers;
        }

        public void CreateChatRoom(string roomId, string roomName)
        {
            var room = new ChatRoom
            {
                Id = roomId,
                Name = roomName,
                CreatedAt = DateTime.Now,
                Users = new List<string>()
            };

            _chatRooms[roomId] = room;
            Console.WriteLine($"💬 Chat room created: {roomName} ({roomId})");
        }

        public ChatRoom GetChatRoom(string roomId)
        {
            _chatRooms.TryGetValue(roomId, out var room);
            return room;
        }

        public List<ChatRoom> GetAllChatRooms()
        {
            return new List<ChatRoom>(_chatRooms.Values);
        }

        public async Task BroadcastMessageAsync(NetworkMessage message, string excludeUserId = null)
        {
            var tasks = new List<Task>();

            foreach (var client in _clients)
            {
                if (client.IsConnected && client.UserId != excludeUserId)
                {
                    tasks.Add(client.SendMessageAsync(message));
                }
            }

            await Task.WhenAll(tasks);
        }

        public void BroadcastSystemMessage(string message)
        {
            var systemMessage = new NetworkMessage
            {
                Type = "system_message",
                SenderId = "system",
                SenderName = "System",
                Content = message,
                Timestamp = DateTime.Now
            };

            _ = BroadcastMessageAsync(systemMessage);
        }

        public void RemoveClient(ClientHandler handler)
        {
            _clients.Remove(handler);
        }
    }

    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly ChatServer _server;
        private NetworkStream _stream;
        private string _userId;
        private string _username;

        public string UserId => _userId;
        public string Username => _username;
        public bool IsConnected => _client?.Connected == true;

        public ClientHandler(TcpClient client, ChatServer server)
        {
            _client = client;
            _server = server;
        }

        public async Task HandleClientAsync()
        {
            try
            {
                _stream = _client.GetStream();
                var buffer = new byte[4096];

                while (_client.Connected)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    await ProcessMessageAsync(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Client error ({_username}): {ex.Message}");
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

                switch (message.Type)
                {
                    case "login":
                        await HandleLoginAsync(message);
                        break;
                    case "message":
                        await HandleMessageAsync(message);
                        break;
                    case "create_chat":
                        await HandleCreateChatAsync(message);
                        break;
                    case "join_chat":
                        await HandleJoinChatAsync(message);
                        break;
                    case "typing":
                        await HandleTypingAsync(message);
                        break;
                    case "get_users":
                        await HandleGetUsersAsync();
                        break;
                    case "get_chats":
                        await HandleGetChatsAsync();
                        break;
                    case "private_message":
                        await HandlePrivateMessageAsync(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing message: {ex.Message}");
            }
        }

        private async Task HandleLoginAsync(NetworkMessage message)
        {
            _userId = message.SenderId ?? Guid.NewGuid().ToString();
            _username = message.Content ?? $"User_{_userId.Substring(0, 4)}";

            // Регистрируем пользователя
            _server.RegisterUser(_userId, _username, this);

            // Отправляем подтверждение
            var response = new NetworkMessage
            {
                Type = "login_success",
                SenderId = "server",
                SenderName = "Server",
                Content = _userId,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(response);

            // Отправляем список пользователей
            await SendOnlineUsersListAsync();

            // Отправляем список чатов
            await SendChatRoomsListAsync();
        }

        private async Task HandleMessageAsync(NetworkMessage message)
        {
            // Добавляем отправителя
            message.SenderName = _username;
            message.Timestamp = DateTime.Now;

            // Если это сообщение в комнату
            if (!string.IsNullOrEmpty(message.ChatId))
            {
                var room = _server.GetChatRoom(message.ChatId);
                if (room != null)
                {
                    // Отправляем всем в этой комнате
                    await BroadcastToRoomAsync(message, room);

                    // Логируем сообщение
                    Console.WriteLine($"💬 [{room.Name}] {_username}: {message.Content}");
                }
            }
            else
            {
                // Отправляем всем
                await _server.BroadcastMessageAsync(message, _userId);

                // Логируем сообщение
                Console.WriteLine($"📢 {_username}: {message.Content}");
            }
        }

        private async Task HandlePrivateMessageAsync(NetworkMessage message)
        {
            var targetUserId = message.ChatId; // В этом случае ChatId содержит ID получателя
            var targetUser = _server.GetUser(targetUserId);

            if (targetUser != null && targetUser.IsOnline)
            {
                // Отправляем только получателю
                message.SenderName = _username;
                message.Timestamp = DateTime.Now;

                await targetUser.Handler.SendMessageAsync(message);

                // Также отправляем копию отправителю для подтверждения
                message.Type = "message_sent";
                await SendMessageAsync(message);

                Console.WriteLine($"🔒 {_username} -> {targetUser.Username}: {message.Content}");
            }
            else
            {
                // Пользователь оффлайн
                var errorMessage = new NetworkMessage
                {
                    Type = "error",
                    SenderId = "server",
                    SenderName = "Server",
                    Content = $"Пользователь {targetUserId} не в сети",
                    Timestamp = DateTime.Now
                };

                await SendMessageAsync(errorMessage);
            }
        }

        private async Task HandleCreateChatAsync(NetworkMessage message)
        {
            var roomId = Guid.NewGuid().ToString();
            var roomName = message.Content;

            _server.CreateChatRoom(roomId, roomName);

            // Добавляем создателя в комнату
            var room = _server.GetChatRoom(roomId);
            room.Users.Add(_userId);

            // Отправляем подтверждение
            var response = new NetworkMessage
            {
                Type = "chat_created",
                SenderId = "server",
                SenderName = "Server",
                Content = roomId,
                Data = JsonSerializer.Serialize(room),
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(response);

            // Уведомляем всех о новой комнате
            await _server.BroadcastMessageAsync(new NetworkMessage
            {
                Type = "chat_list_updated",
                SenderId = "server",
                SenderName = "Server",
                Timestamp = DateTime.Now
            });
        }

        private async Task HandleJoinChatAsync(NetworkMessage message)
        {
            var roomId = message.ChatId;
            var room = _server.GetChatRoom(roomId);

            if (room != null && !room.Users.Contains(_userId))
            {
                room.Users.Add(_userId);

                // Уведомляем всех в комнате
                await BroadcastToRoomAsync(new NetworkMessage
                {
                    Type = "user_joined",
                    SenderId = "server",
                    SenderName = "Server",
                    Content = $"{_username} присоединился к чату",
                    ChatId = roomId,
                    Timestamp = DateTime.Now
                }, room);

                // Отправляем историю комнаты (упрощенно)
                var historyMessage = new NetworkMessage
                {
                    Type = "chat_history",
                    SenderId = "server",
                    SenderName = "Server",
                    Content = $"Добро пожаловать в чат '{room.Name}'",
                    ChatId = roomId,
                    Timestamp = DateTime.Now
                };

                await SendMessageAsync(historyMessage);
            }
        }

        private async Task HandleTypingAsync(NetworkMessage message)
        {
            // Пересылаем статус печатания в комнату
            if (!string.IsNullOrEmpty(message.ChatId))
            {
                var room = _server.GetChatRoom(message.ChatId);
                if (room != null)
                {
                    message.SenderName = _username;
                    await BroadcastToRoomAsync(message, room, _userId);
                }
            }
        }

        private async Task HandleGetUsersAsync()
        {
            await SendOnlineUsersListAsync();
        }

        private async Task HandleGetChatsAsync()
        {
            await SendChatRoomsListAsync();
        }

        private async Task SendOnlineUsersListAsync()
        {
            var onlineUsers = _server.GetOnlineUsers();
            var usersList = JsonSerializer.Serialize(onlineUsers);

            var response = new NetworkMessage
            {
                Type = "users_list",
                SenderId = "server",
                SenderName = "Server",
                Content = usersList,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(response);
        }

        private async Task SendChatRoomsListAsync()
        {
            var chatRooms = _server.GetAllChatRooms();
            var roomsList = JsonSerializer.Serialize(chatRooms);

            var response = new NetworkMessage
            {
                Type = "chats_list",
                SenderId = "server",
                SenderName = "Server",
                Content = roomsList,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(response);
        }

        private async Task BroadcastToRoomAsync(NetworkMessage message, ChatRoom room, string excludeUserId = null)
        {
            foreach (var userId in room.Users)
            {
                if (userId == excludeUserId) continue;

                var user = _server.GetUser(userId);
                if (user != null && user.IsOnline)
                {
                    await user.Handler.SendMessageAsync(message);
                }
            }
        }

        public async Task SendMessageAsync(NetworkMessage message)
        {
            try
            {
                if (_stream != null && _client.Connected)
                {
                    var json = JsonSerializer.Serialize(message);
                    var data = Encoding.UTF8.GetBytes(json + "\n"); // Добавляем разделитель
                    await _stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending message to {_username}: {ex.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            try
            {
                if (!string.IsNullOrEmpty(_userId))
                {
                    _server.UnregisterUser(_userId);
                }

                _server.RemoveClient(this);
                _stream?.Close();
                _client?.Close();

                Console.WriteLine($"👋 Client disconnected: {_username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error during disconnect: {ex.Message}");
            }
        }
    }

    public class NetworkMessage
    {
        public string Type { get; set; }          // Тип сообщения
        public string SenderId { get; set; }      // ID отправителя
        public string SenderName { get; set; }    // Имя отправителя
        public string Content { get; set; }       // Содержимое
        public string ChatId { get; set; }        // ID чата/комнаты
        public string Data { get; set; }          // Дополнительные данные
        public DateTime Timestamp { get; set; }   // Время отправки
    }

    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
        public ClientHandler Handler { get; set; }
    }

    public class ChatRoom
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Users { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}