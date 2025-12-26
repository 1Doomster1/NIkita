using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NIkita;


namespace NikitaMicrosoft
{
    public class AppViewModel : INotifyPropertyChanged
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private string _username;
        private bool _isConnected;
        private Chat _selectedChat;
        private string _messageText;
        private string _searchText;
        private string _typingStatus;
        private bool _isTyping;

        public event EventHandler MessageAdded;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChatSelected));
            }
        }

        public bool IsChatSelected => SelectedChat != null;

        public Chat SelectedChat
        {
            get => _selectedChat;
            set
            {
                if (_selectedChat != value)
                {
                    _selectedChat = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsChatSelected));

                    if (value != null)
                    {
                        value.UnreadCount = 0;
                        SendTypingStatus(false);
                    }
                }
            }
        }

        public string MessageText
        {
            get => _messageText;
            set
            {
                _messageText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendMessage));

                if (!string.IsNullOrEmpty(value) && SelectedChat != null)
                {
                    SendTypingStatus(true);
                    _typingTimer?.Dispose();
                    _typingTimer = new Timer(_ => SendTypingStatus(false), null, 2000, Timeout.Infinite);
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public string TypingStatus
        {
            get => _typingStatus;
            set
            {
                _typingStatus = value;
                OnPropertyChanged();
            }
        }

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) && SelectedChat != null;

        public ObservableCollection<Chat> Chats { get; } = new ObservableCollection<Chat>();
        public ObservableCollection<User> OnlineUsers { get; } = new ObservableCollection<User>();

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand NewChatCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand AttachFileCommand { get; }
        public ICommand AttachImageCommand { get; }
        public ICommand DownloadFileCommand { get; }
        public ICommand StartPrivateChatCommand { get; }

        private Timer _typingTimer;

        public AppViewModel()
        {
            Username = $"User_{new Random().Next(1000, 9999)}";

            ConnectCommand = new RelayCommand(async () => await ConnectToServerAsync());
            DisconnectCommand = new RelayCommand(DisconnectFromServer);
            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => CanSendMessage);
            NewChatCommand = new RelayCommand(CreateNewChat);
            ClearSearchCommand = new RelayCommand(() => SearchText = "");
            AttachFileCommand = new RelayCommand(AttachFile);
            AttachImageCommand = new RelayCommand(AttachImage);
            DownloadFileCommand = new RelayCommand<Message>(DownloadFile);
            StartPrivateChatCommand = new RelayCommand<User>(StartPrivateChat);

            // Создаем тестовые данные
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            // Тестовые чаты
            var chat1 = new Chat
            {
                Id = "1",
                Name = "Алексей",
                LastMessage = "Привет! Как дела?",
                LastMessageTime = DateTime.Now.AddMinutes(-30),
                IsOnline = true,
                UnreadCount = 2
            };

            var chat2 = new Chat
            {
                Id = "2",
                Name = "Мария",
                LastMessage = "Отправлю файл завтра",
                LastMessageTime = DateTime.Now.AddHours(-2),
                IsOnline = false,
                UnreadCount = 0
            };

            var chat3 = new Chat
            {
                Id = "3",
                Name = "Общий чат",
                LastMessage = "Добро пожаловать в общий чат!",
                LastMessageTime = DateTime.Now.AddDays(-1),
                IsOnline = true,
                UnreadCount = 5
            };

            Chats.Add(chat1);
            Chats.Add(chat2);
            Chats.Add(chat3);

            // Тестовые сообщения для первого чата
            chat1.Messages.Add(new Message
            {
                Id = "1",
                Sender = "Алексей",
                Content = "Привет!",
                Timestamp = DateTime.Now.AddMinutes(-45),
                IsMyMessage = false,
                Type = MessageType.Text,
                Status = MessageStatus.Read
            });

            chat1.Messages.Add(new Message
            {
                Id = "2",
                Sender = Username,
                Content = "Привет! Как дела?",
                Timestamp = DateTime.Now.AddMinutes(-30),
                IsMyMessage = true,
                Type = MessageType.Text,
                Status = MessageStatus.Delivered
            });

            // Тестовые онлайн пользователи
            OnlineUsers.Add(new User { Id = "1", Username = "Алексей", IsOnline = true });
            OnlineUsers.Add(new User { Id = "2", Username = "Мария", IsOnline = false });
            OnlineUsers.Add(new User { Id = "3", Username = "Иван", IsOnline = true });
            OnlineUsers.Add(new User { Id = "4", Username = "Ольга", IsOnline = true });
            OnlineUsers.Add(new User { Id = "5", Username = "Дмитрий", IsOnline = false });
        }

        public async Task ConnectToServerAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", 8888);
                _stream = _client.GetStream();
                IsConnected = true;

                var loginMessage = new NetworkMessage
                {
                    Type = "login",
                    SenderId = Guid.NewGuid().ToString(),
                    Content = Username,
                    Timestamp = DateTime.Now
                };

                await SendNetworkMessageAsync(loginMessage);

                _receiveThread = new Thread(ReceiveMessages);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                MessageBox.Show("Успешно подключено к серверу!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void DisconnectFromServer()
        {
            try
            {
                IsConnected = false;
                _receiveThread?.Abort();
                _stream?.Close();
                _client?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || SelectedChat == null)
                return;

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Sender = Username,
                Content = MessageText,
                Timestamp = DateTime.Now,
                IsMyMessage = true,
                Type = MessageType.Text,
                Status = MessageStatus.Sent
            };

            SelectedChat.Messages.Add(message);
            SelectedChat.LastMessage = MessageText;
            SelectedChat.LastMessageTime = DateTime.Now;

            MessageAdded?.Invoke(this, EventArgs.Empty);
            MessageText = "";

            // Отправляем на сервер
            if (IsConnected && _stream != null)
            {
                var networkMessage = new NetworkMessage
                {
                    Type = "message",
                    SenderId = Username,
                    Content = message.Content,
                    ChatId = SelectedChat.Id,
                    Timestamp = DateTime.Now
                };

                await SendNetworkMessageAsync(networkMessage);
            }
        }

        private void CreateNewChat()
        {
            var dialog = new InputDialog("Новый чат", "Введите имя собеседника:");
            if (dialog.ShowDialog() == true)
            {
                var chatName = dialog.Answer;

                var newChat = new Chat
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = chatName,
                    LastMessage = "Нет сообщений",
                    LastMessageTime = DateTime.Now,
                    IsOnline = true,
                    UnreadCount = 0
                };

                Chats.Insert(0, newChat);
                SelectedChat = newChat;
            }
        }

        private void AttachFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл",
                Filter = "Все файлы (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                var fileName = Path.GetFileName(filePath);

                if (SelectedChat != null)
                {
                    var message = new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Sender = Username,
                        Content = fileName,
                        Timestamp = DateTime.Now,
                        IsMyMessage = true,
                        Type = MessageType.File,
                        Status = MessageStatus.Sent,
                        FilePath = filePath
                    };

                    SelectedChat.Messages.Add(message);
                    SelectedChat.LastMessage = $"Файл: {fileName}";
                    SelectedChat.LastMessageTime = DateTime.Now;

                    MessageAdded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void AttachImage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var imagePath = dialog.FileName;
                var fileName = Path.GetFileName(imagePath);

                if (SelectedChat != null)
                {
                    var message = new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Sender = Username,
                        Content = fileName,
                        Timestamp = DateTime.Now,
                        IsMyMessage = true,
                        Type = MessageType.Image,
                        Status = MessageStatus.Sent,
                        ImagePath = imagePath,
                        FilePath = imagePath
                    };

                    SelectedChat.Messages.Add(message);
                    SelectedChat.LastMessage = $"Изображение: {fileName}";
                    SelectedChat.LastMessageTime = DateTime.Now;

                    MessageAdded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void DownloadFile(Message message)
        {
            try
            {
                var filePath = message.Type == MessageType.Image ? message.ImagePath : message.FilePath;

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    MessageBox.Show("Файл не найден", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Title = "Сохранить файл",
                    FileName = Path.GetFileName(filePath),
                    Filter = "Все файлы (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(filePath, saveDialog.FileName, true);
                    MessageBox.Show("Файл успешно сохранен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartPrivateChat(User user)
        {
            var existingChat = Chats.FirstOrDefault(c => c.Name == user.Username);

            if (existingChat != null)
            {
                SelectedChat = existingChat;
                return;
            }

            var newChat = new Chat
            {
                Id = Guid.NewGuid().ToString(),
                Name = user.Username,
                LastMessage = "Начните общение",
                LastMessageTime = DateTime.Now,
                IsOnline = user.IsOnline,
                UnreadCount = 0
            };

            Chats.Insert(0, newChat);
            SelectedChat = newChat;
        }

        private void SendTypingStatus(bool isTyping)
        {
            if (IsConnected && SelectedChat != null && _stream != null)
            {
                _isTyping = isTyping;

                var typingMessage = new NetworkMessage
                {
                    Type = "typing",
                    SenderId = Username,
                    ChatId = SelectedChat.Id,
                    Content = isTyping ? "typing" : "stopped",
                    Timestamp = DateTime.Now
                };

                _ = Task.Run(async () => await SendNetworkMessageAsync(typingMessage));
            }
        }

        private async Task SendNetworkMessageAsync(NetworkMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var data = Encoding.UTF8.GetBytes(json + "\n");
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки: {ex.Message}");
            }
        }

        private async void ReceiveMessages()
        {
            var buffer = new byte[4096];

            try
            {
                while (IsConnected && _client.Connected)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    await ProcessReceivedMessageAsync(json);
                }
            }
            catch (Exception ex)
            {
                if (IsConnected)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Ошибка соединения: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DisconnectFromServer();
                    });
                }
            }
        }

        private async Task ProcessReceivedMessageAsync(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<NetworkMessage>(json);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (message.Type)
                    {
                        case "message":
                            HandleReceivedMessage(message);
                            break;
                        case "private_message":
                            HandlePrivateMessage(message);
                            break;
                        case "typing":
                            HandleTypingStatus(message);
                            break;
                        case "users_list":
                            UpdateOnlineUsers(message.Content);
                            break;
                        case "chats_list":
                            UpdateChatsList(message.Content);
                            break;
                        case "system_message":
                            ShowSystemMessage(message.Content);
                            break;
                        case "login_success":
                            Username = message.Content;
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        private void HandleReceivedMessage(NetworkMessage networkMessage)
        {
            var chat = Chats.FirstOrDefault(c => c.Id == networkMessage.ChatId);
            if (chat == null)
            {
                chat = new Chat
                {
                    Id = networkMessage.ChatId,
                    Name = networkMessage.SenderName,
                    IsOnline = true
                };
                Chats.Add(chat);
            }

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Sender = networkMessage.SenderName,
                Content = networkMessage.Content,
                Timestamp = networkMessage.Timestamp,
                IsMyMessage = false,
                Type = MessageType.Text,
                Status = MessageStatus.Delivered
            };

            chat.Messages.Add(message);
            chat.LastMessage = networkMessage.Content;
            chat.LastMessageTime = networkMessage.Timestamp;

            if (chat != SelectedChat)
            {
                chat.UnreadCount++;
            }

            MessageAdded?.Invoke(this, EventArgs.Empty);
        }

        private void HandlePrivateMessage(NetworkMessage networkMessage)
        {
            var senderName = networkMessage.SenderName;
            var chat = Chats.FirstOrDefault(c => c.Name == senderName);

            if (chat == null)
            {
                chat = new Chat
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = senderName,
                    IsOnline = true
                };
                Chats.Add(chat);
            }

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Sender = senderName,
                Content = networkMessage.Content,
                Timestamp = networkMessage.Timestamp,
                IsMyMessage = false,
                Type = MessageType.Text,
                Status = MessageStatus.Delivered
            };

            chat.Messages.Add(message);
            chat.LastMessage = networkMessage.Content;
            chat.LastMessageTime = networkMessage.Timestamp;

            if (chat != SelectedChat)
            {
                chat.UnreadCount++;
            }

            MessageAdded?.Invoke(this, EventArgs.Empty);
        }

        private void HandleTypingStatus(NetworkMessage networkMessage)
        {
            if (SelectedChat != null && SelectedChat.Id == networkMessage.ChatId)
            {
                TypingStatus = networkMessage.Content == "typing"
                    ? $"{networkMessage.SenderName} печатает..."
                    : "";
            }
        }

        private void UpdateOnlineUsers(string usersJson)
        {
            try
            {
                var users = JsonSerializer.Deserialize<User[]>(usersJson);
                OnlineUsers.Clear();

                foreach (var user in users)
                {
                    OnlineUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления пользователей: {ex.Message}");
            }
        }

        private void UpdateChatsList(string chatsJson)
        {
            try
            {
                var chats = JsonSerializer.Deserialize<Chat[]>(chatsJson);
                Chats.Clear();

                foreach (var chat in chats)
                {
                    Chats.Add(chat);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления чатов: {ex.Message}");
            }
        }

        private void ShowSystemMessage(string message)
        {
            MessageBox.Show(message, "Системное сообщение",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SelectChat(Chat chat)
        {
            SelectedChat = chat;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}