using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using NIkita;
using NikitaMicrosoft;

namespace NikitaMessenger
{
    public class AppViewModel : INotifyPropertyChanged
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private string _username;
        private bool _isConnected;
        private ChatModel _selectedChat;
        private string _messageText;
        private string _searchText;
        private string _typingStatus;
        private bool _isTyping;
        private string _avatarPath;

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

        public ChatModel SelectedChat
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

        public string AvatarPath
        {
            get => _avatarPath;
            set
            {
                _avatarPath = value;
                OnPropertyChanged();
            }
        }

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) && SelectedChat != null;

        public ObservableCollection<ChatModel> Chats { get; } = new ObservableCollection<ChatModel>();
        public ObservableCollection<UserModel> OnlineUsers { get; } = new ObservableCollection<UserModel>();

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand NewChatCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand AttachFileCommand { get; }
        public ICommand AttachImageCommand { get; }
        public ICommand DownloadFileCommand { get; }
        public ICommand StartPrivateChatCommand { get; }
        public ICommand ChangeAvatarCommand { get; }

        private Timer _typingTimer;

        public AppViewModel(NikitaDbContext context)
        {
            Username = $"User_{new Random().Next(1000, 9999)}";

            ConnectCommand = new RelayCommand(async () => await ConnectToServerAsync());
            DisconnectCommand = new RelayCommand(DisconnectFromServer);
            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(context), () => CanSendMessage);
            NewChatCommand = new RelayCommand(() => CreateNewChat(context));
            ClearSearchCommand = new RelayCommand(() => SearchText = "");
            AttachFileCommand = new RelayCommand(() => AttachFile(context));
            AttachImageCommand = new RelayCommand(() => AttachImage(context));
            DownloadFileCommand = new RelayCommand<Message>(DownloadFile);
            StartPrivateChatCommand = new RelayCommand<UserModel>(StartPrivateChat);
            ChangeAvatarCommand = new RelayCommand(ChangeAvatar);
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

                // Сохраняем аватар перед выходом
                SaveAvatar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendMessageAsync(NikitaDbContext context)
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
                Status = MessageStatus.Sent,
                ChatId = SelectedChat.Id
            };

            SelectedChat.Messages.Add(message);
            SelectedChat.LastMessage = MessageText;
            SelectedChat.LastMessageTime = DateTime.Now;

            MessageAdded?.Invoke(this, EventArgs.Empty);
            MessageText = "";

            if (IsConnected && _stream != null)
            {
                var networkMessage = new NetworkMessage
                {
                    Type = SelectedChat.Name == "Общий чат" ? "message" : "private_message",
                    SenderId = Username,
                    SenderName = Username,
                    Content = message.Content,
                    ChatId = SelectedChat.Name == "Общий чат" ? "general" : SelectedChat.Name,
                    Timestamp = DateTime.Now
                };

                await SendNetworkMessageAsync(networkMessage);
            }
        }

        private void CreateNewChat(NikitaDbContext context)
        {
            var dialog = new InputDialog("Новый чат", "Введите имя собеседника:");
            if (dialog.ShowDialog() == true)
            {
                var chatName = dialog.Answer;

                var newChat = new ChatModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = chatName,
                    LastMessage = "Нет сообщений",
                    LastMessageTime = DateTime.Now,
                    IsOnline = false,
                    UnreadCount = 0
                };

                Chats.Insert(0, newChat);
                SelectedChat = newChat;
            }
        }

        private void AttachFile(NikitaDbContext context)
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
                        FilePath = filePath,
                        ChatId = SelectedChat.Id
                    };

                    //SelectedChat.Messages.Add(message);
                    //context.messages.Add(message);
                    SelectedChat.LastMessage = $"Файл: {fileName}";
                    SelectedChat.LastMessageTime = DateTime.Now;

                    MessageAdded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void AttachImage(NikitaDbContext context)
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

        private void StartPrivateChat(UserModel user)
        {
            var existingChat = Chats.FirstOrDefault(c => c.Name == user.Username);

            if (existingChat != null)
            {
                SelectedChat = existingChat;
                return;
            }

            var newChat = new ChatModel
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

        private void ChangeAvatar()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите аватар",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    AvatarPath = dialog.FileName;
                    SaveAvatar();

                    MessageBox.Show("Аватар успешно изменен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке аватара: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAvatar()
        {
            try
            {
                if (!string.IsNullOrEmpty(AvatarPath))
                {
                    File.WriteAllText("avatar.txt", AvatarPath);
                }
            }
            catch { }
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
                    SenderName = Username,
                    ChatId = SelectedChat.Name,
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
                    var messages = json.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var message in messages)
                    {
                        await ProcessReceivedMessageAsync(message);
                    }
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
                        case "public_message":
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
            var chat = Chats.FirstOrDefault(c => c.Id == networkMessage.ChatId ||
                (c.Name == "Общий чат" && networkMessage.ChatId == "general"));

            if (chat == null && networkMessage.SenderName != Username)
            {
                chat = new ChatModel
                {
                    Id = networkMessage.ChatId ?? Guid.NewGuid().ToString(),
                    Name = networkMessage.ChatId == "general" ? "Общий чат" : networkMessage.SenderName,
                    IsOnline = true
                };
                Chats.Insert(0, chat);
            }

            if (chat != null)
            {
                var message = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    Sender = networkMessage.SenderName,
                    Content = networkMessage.Content,
                    Timestamp = networkMessage.Timestamp,
                    IsMyMessage = networkMessage.SenderName == Username,
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
        }

        private void HandlePrivateMessage(NetworkMessage networkMessage)
        {
            if (networkMessage.SenderName == Username) return;

            var chat = Chats.FirstOrDefault(c => c.Name == networkMessage.SenderName);

            if (chat == null)
            {
                chat = new ChatModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = networkMessage.SenderName,
                    IsOnline = true
                };
                Chats.Insert(0, chat);
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
                chat.IsOnline = true;
            }

            MessageAdded?.Invoke(this, EventArgs.Empty);
        }

        private void HandleTypingStatus(NetworkMessage networkMessage)
        {
            if (SelectedChat != null && SelectedChat.Name == networkMessage.SenderName)
            {
                networkMessage.Content = "typing";
                SelectedChat.TypingStatus = networkMessage.Content == "typing" ?
                    $"{networkMessage.SenderName} печатает..." : "";
            }
        }

        private void UpdateOnlineUsers(string usersJson)
        {
            try
            {
                var users = JsonSerializer.Deserialize<UserModel[]>(usersJson);
                OnlineUsers.Clear();

                foreach (var user in users)
                {
                    if (user.Username != Username)
                    {
                        OnlineUsers.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления пользователей: {ex.Message}");
            }
        }

        private void ShowSystemMessage(string message)
        {
            MessageBox.Show(message, "Системное сообщение",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SelectChat(ChatModel chat)
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