using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NikitaMessenger
{
    public class NetworkClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private bool _isConnected = false;
        public string Username { get; set; }

        public event Action<string, string> MessageReceived; // chatId, message
        public event Action<string> UserConnected; // username
        public event Action<string> UserDisconnected; // username
        public event Action<bool> ConnectionStatusChanged; // isConnected

        public bool IsConnected => _isConnected && _tcpClient?.Connected == true;

        public async Task<bool> ConnectAsync(string server = "127.0.0.1", int port = 8888)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(server, port);
                _stream = _tcpClient.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                _isConnected = true;
                ConnectionStatusChanged?.Invoke(true);

                // Запускаем фоновую задачу для чтения сообщений
                _ = Task.Run(ReceiveMessagesAsync);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(false);
                return false;
            }
        }

        public async Task LoginAsync(string username)
        {
            if (!IsConnected) return;

            Username = username;
            var loginMessage = new NetworkMessage
            {
                Type = "login",
                Content = username,
                Timestamp = DateTime.Now
            };

            await SendAsync(loginMessage);
        }

        public async Task SendMessageAsync(string chatId, string content)
        {
            if (!IsConnected) return;

            var message = new NetworkMessage
            {
                Type = "message",
                ChatId = chatId,
                SenderName = Username,
                Content = content,
                Timestamp = DateTime.Now
            };

            await SendAsync(message);
        }

        private async Task SendAsync(NetworkMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                await _writer.WriteLineAsync(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
                Disconnect();
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            try
            {
                while (IsConnected)
                {
                    var json = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(json)) continue;

                    var message = JsonSerializer.Deserialize<NetworkMessage>(json);
                    if (message == null) continue;

                    ProcessReceivedMessage(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private void ProcessReceivedMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case "login_success":
                    Console.WriteLine($"Login successful: {message.Content}");
                    break;

                case "message":
                    MessageReceived?.Invoke(message.ChatId, $"{message.SenderName}: {message.Content}");
                    break;

                case "user_connected":
                    UserConnected?.Invoke(message.Content);
                    break;

                case "user_disconnected":
                    UserDisconnected?.Invoke(message.Content);
                    break;
            }
        }

        public void Disconnect()
        {
            try
            {
                _isConnected = false;
                _reader?.Close();
                _writer?.Close();
                _stream?.Close();
                _tcpClient?.Close();

                ConnectionStatusChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect error: {ex.Message}");
            }
        }
    }
}