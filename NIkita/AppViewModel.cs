using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace NIkita
{
    public class AppViewModel : INotifyPropertyChanged
    {
        private Chat _selectedChat;
        private string _messageText;

        public ObservableCollection<Chat> Chats { get; set; }
        public event EventHandler MessageAdded;

        public Chat SelectedChat
        {
            get => _selectedChat;
            set
            {
                _selectedChat = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChatSelected));
                OnPropertyChanged(nameof(IsNoChatSelected));
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
            }
        }

        public bool IsChatSelected => SelectedChat != null;
        public bool IsNoChatSelected => !IsChatSelected;
        public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) && IsChatSelected;

        public ICommand SendMessageCommand { get; }
        public ICommand AttachFileCommand { get; }
        public ICommand AttachImageCommand { get; }
        public ICommand DownloadFileCommand { get; }
        public ICommand NewChatCommand { get; }

        public AppViewModel()
        {
            Chats = new ObservableCollection<Chat>();

            SendMessageCommand = new RelayCommand(SendMessageExecute, () => CanSendMessage);
            AttachFileCommand = new RelayCommand(AttachFileExecute);
            AttachImageCommand = new RelayCommand(AttachImageExecute);
            DownloadFileCommand = new RelayCommand<Message>(DownloadFileExecute);
            NewChatCommand = new RelayCommand(NewChatExecute);

            LoadTestData();
        }

        private void LoadTestData()
        {
            var chat1 = new Chat
            {
                Id = "1",
                Name = "Анна Петрова",
                LastMessage = "Привет! Как дела?",
                LastMessageTime = DateTime.Now.AddHours(-1),
                IsOnline = true
            };

            chat1.Messages.Add(new Message
            {
                Id = "1",
                Sender = "Анна Петрова",
                Content = "Привет! Как дела?",
                Timestamp = DateTime.Now.AddHours(-2),
                IsMyMessage = false,
                Type = MessageType.Text,
                Status = MessageStatus.Read
            });

            chat1.Messages.Add(new Message
            {
                Id = "2",
                Sender = "Вы",
                Content = "Привет! Все отлично, спасибо!",
                Timestamp = DateTime.Now.AddHours(-1),
                IsMyMessage = true,
                Type = MessageType.Text,
                Status = MessageStatus.Read
            });

            var chat2 = new Chat
            {
                Id = "2",
                Name = "Иван Сидоров",
                LastMessage = "Отправляю документы",
                LastMessageTime = DateTime.Now.AddDays(-1),
                IsOnline = false,
                UnreadCount = 0
            };

            chat2.Messages.Add(new Message
            {
                Id = "3",
                Sender = "Иван Сидоров",
                Content = "Отчет за март.pdf",
                Timestamp = DateTime.Now.AddDays(-2),
                IsMyMessage = false,
                Type = MessageType.File,
                Status = MessageStatus.Read,
                ImagePath = @"C:\Temp\report.pdf"
            });

            Chats.Add(chat1);
            Chats.Add(chat2);
        }

        private void SendMessageExecute()
        {
            if (!CanSendMessage) return;

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "Вы",
                Content = MessageText,
                Timestamp = DateTime.Now,
                IsMyMessage = true,
                Type = MessageType.Text,
                Status = MessageStatus.Sent
            };

            SelectedChat.Messages.Add(message);
            SelectedChat.LastMessage = MessageText;
            SelectedChat.LastMessageTime = DateTime.Now;

            MessageText = string.Empty;
            MessageAdded?.Invoke(this, EventArgs.Empty);

            Task.Delay(1000).ContinueWith(t =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var reply = new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Sender = SelectedChat.Name,
                        Content = "Получил ваше сообщение!",
                        Timestamp = DateTime.Now.AddSeconds(1),
                        IsMyMessage = false,
                        Type = MessageType.Text,
                        Status = MessageStatus.Read
                    };

                    SelectedChat.Messages.Add(reply);
                    SelectedChat.LastMessage = reply.Content;
                    SelectedChat.LastMessageTime = reply.Timestamp;
                    SelectedChat.UnreadCount++;

                    MessageAdded?.Invoke(this, EventArgs.Empty);
                });
            });
        }

        private void AttachFileExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Все файлы (*.*)|*.*",
                Title = "Выберите файл для отправки"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SendFile(openFileDialog.FileName);
            }
        }

        private void AttachImageExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SendImage(openFileDialog.FileName);
            }
        }

        private void DownloadFileExecute(Message message)
        {
            if (message == null || string.IsNullOrEmpty(message.ImagePath)) return;

            var saveFileDialog = new SaveFileDialog();

            if (message.Type == MessageType.File)
            {
                saveFileDialog.FileName = Path.GetFileName(message.ImagePath);
                saveFileDialog.Filter = "Все файлы (*.*)|*.*";
            }
            else if (message.Type == MessageType.Image)
            {
                saveFileDialog.FileName = Path.GetFileName(message.ImagePath);
                saveFileDialog.Filter = "Изображения (*.jpg;*.png)|*.jpg;*.png";
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                if (File.Exists(message.ImagePath))
                {
                    File.Copy(message.ImagePath, saveFileDialog.FileName, true);
                    MessageBox.Show("Файл успешно сохранен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                    MessageBox.Show("Файл не найден", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                
            }
        }

        private void NewChatExecute()
        {
            var inputDialog = new InputDialog("Создать новый чат", "Введите имя собеседника:");

            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.Answer))
            {
                var newChat = new Chat
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = inputDialog.Answer,
                    LastMessage = "Чат создан",
                    LastMessageTime = DateTime.Now,
                    IsOnline = true,
                    UnreadCount = 0
                };

                Chats.Add(newChat);
                SelectedChat = newChat;
            }
        }

        public void SendFile(string filePath)
        {
            if (!IsChatSelected || !File.Exists(filePath)) return;

            var fileName = Path.GetFileName(filePath);
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "Вы",
                Content = fileName,
                Timestamp = DateTime.Now,
                IsMyMessage = true,
                Type = MessageType.File,
                Status = MessageStatus.Sent,
                ImagePath = filePath
            };

            SelectedChat.Messages.Add(message);
            SelectedChat.LastMessage = $"Файл: {fileName}";
            SelectedChat.LastMessageTime = DateTime.Now;

            MessageAdded?.Invoke(this, EventArgs.Empty);
        }

        public void SendImage(string imagePath)
        {
            if (!IsChatSelected || !File.Exists(imagePath)) return;

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "Вы",
                Content = "Изображение",
                Timestamp = DateTime.Now,
                IsMyMessage = true,
                Type = MessageType.Image,
                Status = MessageStatus.Sent,
                ImagePath = imagePath
            };

            SelectedChat.Messages.Add(message);
            SelectedChat.LastMessage = "Изображение";
            SelectedChat.LastMessageTime = DateTime.Now;

            MessageAdded?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}