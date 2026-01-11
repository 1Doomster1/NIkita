using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace NikitaMessenger
{
    public partial class MainWindow : Window
    {
        // Модель сообщения
        public class Message
        {
            public string Text { get; set; }
            public string Sender { get; set; }
            public DateTime Time { get; set; }
            public bool IsMyMessage { get; set; }
            public string AvatarText { get; set; }
            public Brush AvatarColor { get; set; }
        }

        // Модель чата
        public class Chat
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string LastMessage { get; set; }
            public DateTime LastMessageTime { get; set; }
            public bool IsOnline { get; set; }
            public int UnreadCount { get; set; }
            public string AvatarText { get; set; }
            public Brush AvatarColor { get; set; }
            public List<Message> Messages { get; set; } = new List<Message>();
        }

        // Модель пользователя
        public class User
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public bool IsOnline { get; set; }
            public string AvatarText { get; set; }
            public Brush AvatarColor { get; set; }
        }

        // Коллекции данных
        private List<Chat> chats = new List<Chat>();
        private List<User> onlineUsers = new List<User>();
        private Chat currentChat = null;
        private Random random = new Random();
        private bool isConnected = false;

        // Цвета для аватаров
        private Brush[] avatarColors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(0, 132, 255)),   // Синий
            new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Зеленый
            new SolidColorBrush(Color.FromRgb(255, 87, 34)),    // Оранжевый
            new SolidColorBrush(Color.FromRgb(156, 39, 176)),   // Фиолетовый
            new SolidColorBrush(Color.FromRgb(233, 30, 99)),    // Розовый
            new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Голубой
            new SolidColorBrush(Color.FromRgb(255, 193, 7)),    // Желтый
            new SolidColorBrush(Color.FromRgb(0, 150, 136))     // Бирюзовый
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeTestData();
            UpdateOnlineUsersCount();
            UpdateChatsList();
        }

        private void InitializeTestData()
        {
            // Тестовые чаты
            chats = new List<Chat>
            {
                new Chat
                {
                    Id = "1",
                    Name = "Анна Иванова",
                    LastMessage = "Привет! Как дела?",
                    LastMessageTime = DateTime.Now.AddMinutes(-30),
                    IsOnline = true,
                    UnreadCount = 2,
                    AvatarText = "АИ",
                    AvatarColor = avatarColors[0]
                },
                new Chat
                {
                    Id = "2",
                    Name = "Иван Петров",
                    LastMessage = "Встречаемся в 18:00",
                    LastMessageTime = DateTime.Now.AddHours(-2),
                    IsOnline = false,
                    UnreadCount = 0,
                    AvatarText = "ИП",
                    AvatarColor = avatarColors[1]
                },
                new Chat
                {
                    Id = "3",
                    Name = "Мария Сидорова",
                    LastMessage = "Отправил документы",
                    LastMessageTime = DateTime.Now.AddDays(-1),
                    IsOnline = true,
                    UnreadCount = 5,
                    AvatarText = "МС",
                    AvatarColor = avatarColors[2]
                },
                new Chat
                {
                    Id = "4",
                    Name = "Рабочая группа",
                    LastMessage = "Обсудим проект завтра",
                    LastMessageTime = DateTime.Now.AddHours(-5),
                    IsOnline = false,
                    UnreadCount = 0,
                    AvatarText = "РГ",
                    AvatarColor = avatarColors[3]
                },
                new Chat
                {
                    Id = "5",
                    Name = "Алексей Смирнов",
                    LastMessage = "Спасибо за помощь!",
                    LastMessageTime = DateTime.Now.AddDays(-2),
                    IsOnline = false,
                    UnreadCount = 1,
                    AvatarText = "АС",
                    AvatarColor = avatarColors[4]
                }
            };

            // Добавляем тестовые сообщения для первого чата
            chats[0].Messages.AddRange(new[]
            {
                new Message
                {
                    Text = "Привет! Как дела?",
                    Sender = "Анна",
                    Time = DateTime.Now.AddMinutes(-30),
                    IsMyMessage = false,
                    AvatarText = "АИ",
                    AvatarColor = avatarColors[0]
                },
                new Message
                {
                    Text = "Привет! Всё отлично, спасибо! А у тебя как?",
                    Sender = "Я",
                    Time = DateTime.Now.AddMinutes(-25),
                    IsMyMessage = true,
                    AvatarText = "U1",
                    AvatarColor = new SolidColorBrush(Color.FromRgb(0, 132, 255))
                },
                new Message
                {
                    Text = "Тоже всё хорошо! Завтра встретимся?",
                    Sender = "Анна",
                    Time = DateTime.Now.AddMinutes(-20),
                    IsMyMessage = false,
                    AvatarText = "АИ",
                    AvatarColor = avatarColors[0]
                }
            });

            // Тестовые онлайн пользователи
            onlineUsers = new List<User>
            {
                new User { Id = "1", Name = "Анна Иванова", IsOnline = true, AvatarText = "АИ", AvatarColor = avatarColors[0] },
                new User { Id = "3", Name = "Мария Сидорова", IsOnline = true, AvatarText = "МС", AvatarColor = avatarColors[2] },
                new User { Id = "6", Name = "Дмитрий Козлов", IsOnline = true, AvatarText = "ДК", AvatarColor = avatarColors[5] },
                new User { Id = "7", Name = "Елена Новикова", IsOnline = true, AvatarText = "ЕН", AvatarColor = avatarColors[6] },
                new User { Id = "8", Name = "Сергей Волков", IsOnline = true, AvatarText = "СВ", AvatarColor = avatarColors[7] }
            };
        }

        private void UpdateChatsList()
        {
            ChatsPanel.Children.Clear();

            string searchText = SearchTextBox.Text.ToLower();

            foreach (var chat in chats.Where(c =>
                string.IsNullOrEmpty(searchText) ||
                c.Name.ToLower().Contains(searchText)))
            {
                var chatItem = CreateChatItem(chat);
                ChatsPanel.Children.Add(chatItem);
            }
        }

        private Border CreateChatItem(Chat chat)
        {
            var border = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(15),
                Cursor = Cursors.Hand,
                Tag = chat
            };

            // Стили для состояний
            var hoverStyle = new Style(typeof(Border));
            hoverStyle.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(245, 245, 245))));

            var pressedStyle = new Style(typeof(Border));
            pressedStyle.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(235, 235, 235))));

            border.MouseEnter += (s, e) =>
            {
                if (currentChat?.Id != chat.Id)
                    border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            };

            border.MouseLeave += (s, e) =>
            {
                if (currentChat?.Id != chat.Id)
                    border.Background = Brushes.White;
            };

            border.MouseLeftButtonDown += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromRgb(235, 235, 235));
            };

            border.MouseLeftButtonUp += (s, e) =>
            {
                SelectChat(chat);
                border.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // Аватар чата
            var avatarBorder = new Border
            {
                Width = 50,
                Height = 50,
                CornerRadius = new CornerRadius(25),
                Background = chat.AvatarColor,
                Margin = new Thickness(0, 0, 15, 0)
            };

            var avatarText = new TextBlock
            {
                Text = chat.AvatarText,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatarBorder.Child = avatarText;
            Grid.SetColumn(avatarBorder, 0);
            grid.Children.Add(avatarBorder);

            // Информация о чате
            var infoPanel = new StackPanel();
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            var nameText = new TextBlock
            {
                Text = chat.Name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14.5,
                Foreground = Brushes.Black,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            nameRow.Children.Add(nameText);

            if (chat.IsOnline)
            {
                var onlineIndicator = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    Margin = new Thickness(6, 4, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                nameRow.Children.Add(onlineIndicator);
            }

            infoPanel.Children.Add(nameRow);

            var lastMessageText = new TextBlock
            {
                Text = chat.LastMessage,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            infoPanel.Children.Add(lastMessageText);

            // Время и счетчик
            var rightPanel = new StackPanel();
            Grid.SetColumn(rightPanel, 2);
            grid.Children.Add(rightPanel);

            var timeText = new TextBlock
            {
                Text = GetRelativeTime(chat.LastMessageTime),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            rightPanel.Children.Add(timeText);

            if (chat.UnreadCount > 0)
            {
                var unreadBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0, 132, 255)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6, 2, 0, 0),
                    Margin = new Thickness(0, 4, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var unreadText = new TextBlock
                {
                    Text = chat.UnreadCount.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                };

                unreadBadge.Child = unreadText;
                rightPanel.Children.Add(unreadBadge);
            }

            border.Child = grid;
            return border;
        }

        private void UpdateOnlineUsersList()
        {
            OnlineUsersPanel.Children.Clear();

            foreach (var user in onlineUsers)
            {
                var userItem = CreateUserItem(user);
                OnlineUsersPanel.Children.Add(userItem);
            }

            UpdateOnlineUsersCount();
        }

        private Border CreateUserItem(User user)
        {
            var border = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(10),
                Cursor = Cursors.Hand,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 6),
                Tag = user
            };

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = Brushes.White;
            };

            border.MouseLeftButtonUp += (s, e) =>
            {
                // Открыть чат с пользователем или создать новый
                var existingChat = chats.FirstOrDefault(c => c.Name == user.Name);
                if (existingChat != null)
                {
                    SelectChat(existingChat);
                }
                else
                {
                    // Создать новый чат
                    var newChat = new Chat
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = user.Name,
                        LastMessage = "",
                        LastMessageTime = DateTime.Now,
                        IsOnline = user.IsOnline,
                        UnreadCount = 0,
                        AvatarText = user.AvatarText,
                        AvatarColor = user.AvatarColor
                    };

                    chats.Insert(0, newChat);
                    UpdateChatsList();
                    SelectChat(newChat);
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // Аватар пользователя
            var avatarBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = user.AvatarColor,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var avatarText = new TextBlock
            {
                Text = user.AvatarText,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatarBorder.Child = avatarText;
            Grid.SetColumn(avatarBorder, 0);
            grid.Children.Add(avatarBorder);

            // Имя пользователя
            var namePanel = new StackPanel();
            Grid.SetColumn(namePanel, 1);
            grid.Children.Add(namePanel);

            var nameText = new TextBlock
            {
                Text = user.Name,
                FontWeight = FontWeights.Medium,
                FontSize = 14,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            namePanel.Children.Add(nameText);

            // Статус онлайн
            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(statusPanel, 2);
            grid.Children.Add(statusPanel);

            if (user.IsOnline)
            {
                var onlineText = new TextBlock
                {
                    Text = "онлайн",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                statusPanel.Children.Add(onlineText);

                var onlineDot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                statusPanel.Children.Add(onlineDot);
            }
            else
            {
                var offlineText = new TextBlock
                {
                    Text = "офлайн",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153))
                };
                statusPanel.Children.Add(offlineText);
            }

            border.Child = grid;
            return border;
        }

        private void SelectChat(Chat chat)
        {
            currentChat = chat;

            // Обновляем UI для выбранного чата
            UpdateChatSelectionVisual();

            // Показываем заголовок чата
            ChatHeader.Visibility = Visibility.Visible;
            MessagesBorder.Visibility = Visibility.Visible;
            MessageInputBorder.Visibility = Visibility.Visible;
            NoChatSelectedBorder.Visibility = Visibility.Collapsed;

            // Обновляем информацию в заголовке
            ChatNameText.Text = chat.Name;
            ChatStatusText.Text = chat.IsOnline ? "онлайн" : "офлайн";
            ChatStatusText.Foreground = chat.IsOnline ?
                new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                new SolidColorBrush(Color.FromRgb(153, 153, 153));

            ChatAvatarText.Text = chat.AvatarText;
            ChatAvatarBorder.Background = chat.AvatarColor;

            // Очищаем непрочитанные сообщения
            chat.UnreadCount = 0;
            UpdateChatsList();

            // Показываем сообщения
            ShowMessages(chat);
        }

        private void UpdateChatSelectionVisual()
        {
            foreach (var child in ChatsPanel.Children)
            {
                if (child is Border border && border.Tag is Chat chat)
                {
                    if (chat.Id == currentChat?.Id)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                        border.BorderThickness = new Thickness(1);
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    }
                    else
                    {
                        border.Background = Brushes.White;
                        border.BorderThickness = new Thickness(0);
                    }
                }
            }
        }

        private void ShowMessages(Chat chat)
        {
            MessagesPanel.Children.Clear();

            if (chat.Messages.Count == 0)
            {
                // Показываем сообщение о пустом чате
                var emptyText = new TextBlock
                {
                    Text = "Нет сообщений\nНачните диалог первым!",
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                    FontSize = 16,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                MessagesPanel.Children.Add(emptyText);
                return;
            }

            foreach (var message in chat.Messages.OrderBy(m => m.Time))
            {
                var messageControl = CreateMessageControl(message);
                MessagesPanel.Children.Add(messageControl);
            }

            // Прокручиваем к последнему сообщению
            MessagesScrollViewer.ScrollToBottom();
        }

        private Border CreateMessageControl(Message message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("MessageBorderStyle"),
                Tag = message
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Аватар отправителя (только для чужих сообщений)
            if (!message.IsMyMessage)
            {
                var avatarBorder = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Background = message.AvatarColor,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };

                var avatarText = new TextBlock
                {
                    Text = message.AvatarText,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                avatarBorder.Child = avatarText;
                Grid.SetColumn(avatarBorder, 0);
                grid.Children.Add(avatarBorder);
            }

            // Контент сообщения
            var contentPanel = new StackPanel();
            Grid.SetColumn(contentPanel, message.IsMyMessage ? 1 : 1);
            if (!message.IsMyMessage)
                Grid.SetColumnSpan(contentPanel, 2);

            // Текст сообщения
            var textBlock = new TextBlock
            {
                Text = message.Text,
                Foreground = message.IsMyMessage ? Brushes.White : Brushes.Black,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
            contentPanel.Children.Add(textBlock);

            // Время отправки
            var timePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = message.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var timeText = new TextBlock
            {
                Text = message.Time.ToString("HH:mm"),
                FontSize = 11,
                Foreground = message.IsMyMessage ?
                    new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)) :
                    new SolidColorBrush(Color.FromArgb(180, 102, 102, 102))
            };
            timePanel.Children.Add(timeText);

            // Галочка прочтения (только для моих сообщений)
            if (message.IsMyMessage)
            {
                var readIcon = new TextBlock
                {
                    Text = "✓✓",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    Margin = new Thickness(4, 0, 0, 0)
                };
                timePanel.Children.Add(readIcon);
            }

            contentPanel.Children.Add(timePanel);
            grid.Children.Add(contentPanel);

            border.Child = grid;
            return border;
        }

        private void SendMessage()
        {
            if (currentChat == null || string.IsNullOrWhiteSpace(MessageTextBox.Text))
                return;

            var messageText = MessageTextBox.Text.Trim();

            // Создаем новое сообщение
            var message = new Message
            {
                Text = messageText,
                Sender = "Я",
                Time = DateTime.Now,
                IsMyMessage = true,
                AvatarText = "U1",
                AvatarColor = new SolidColorBrush(Color.FromRgb(0, 132, 255))
            };

            // Добавляем в текущий чат
            currentChat.Messages.Add(message);
            currentChat.LastMessage = messageText;
            currentChat.LastMessageTime = DateTime.Now;

            // Обновляем UI
            ShowMessages(currentChat);
            UpdateChatsList();

            // Очищаем поле ввода
            MessageTextBox.Text = "";
            MessageTextBox.Focus();

            // Имитируем ответ через 1-3 секунды
            if (currentChat.IsOnline && random.Next(100) > 30) // 70% шанс на ответ
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(random.Next(1000, 3000));

                    var responses = new[]
                    {
                        "Привет!",
                        "Как дела?",
                        "Интересно...",
                        "Согласен!",
                        "Давай обсудим это позже",
                        "Спасибо за сообщение!",
                        "Хорошо)",
                        "Отличная новость!"
                    };

                    var response = new Message
                    {
                        Text = responses[random.Next(responses.Length)],
                        Sender = currentChat.Name.Split(' ')[0],
                        Time = DateTime.Now,
                        IsMyMessage = false,
                        AvatarText = currentChat.AvatarText,
                        AvatarColor = currentChat.AvatarColor
                    };

                    currentChat.Messages.Add(response);
                    currentChat.LastMessage = response.Text;
                    currentChat.LastMessageTime = DateTime.Now;
                    currentChat.UnreadCount++;

                    Dispatcher.Invoke(() =>
                    {
                        ShowMessages(currentChat);
                        UpdateChatsList();
                    });
                });
            }
        }

        private string GetRelativeTime(DateTime time)
        {
            var span = DateTime.Now - time;

            if (span.TotalMinutes < 1)
                return "только что";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} мин назад";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} ч назад";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} дн назад";

            return time.ToString("dd.MM.yy");
        }

        private void UpdateOnlineUsersCount()
        {
            OnlineUsersCountText.Text = $"({onlineUsers.Count})";
        }

        // Обработчики событий

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            isConnected = !isConnected;

            if (isConnected)
            {
                ConnectButton.Content = "🔌";
                ConnectButton.ToolTip = "Отключиться от сервера";

                // Имитация подключения пользователей
                onlineUsers.AddRange(new[]
                {
                    new User { Id = "9", Name = "Ольга Белова", IsOnline = true, AvatarText = "ОБ", AvatarColor = avatarColors[0] },
                    new User { Id = "10", Name = "Павел Гришин", IsOnline = true, AvatarText = "ПГ", AvatarColor = avatarColors[1] }
                });

                // Обновляем статус некоторых чатов
                foreach (var chat in chats.Where(c => random.Next(100) > 50))
                {
                    chat.IsOnline = true;
                }

                MessageBox.Show("Подключено к серверу!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ConnectButton.Content = "🔌";
                ConnectButton.ToolTip = "Подключиться к серверу";

                // Имитация отключения пользователей
                onlineUsers.RemoveAll(u => u.Id == "9" || u.Id == "10");

                // Обновляем статус чатов
                foreach (var chat in chats)
                {
                    chat.IsOnline = false;
                }

                MessageBox.Show("Отключено от сервера", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            UpdateOnlineUsersList();
            UpdateChatsList();

            if (currentChat != null)
            {
                ChatStatusText.Text = currentChat.IsOnline ? "онлайн" : "офлайн";
                ChatStatusText.Foreground = currentChat.IsOnline ?
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                    new SolidColorBrush(Color.FromRgb(153, 153, 153));
            }
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewChatWindow();
            if (dialog.ShowDialog() == true)
            {
                var newChat = new Chat
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = dialog.ChatName,
                    LastMessage = "Чат создан",
                    LastMessageTime = DateTime.Now,
                    IsOnline = false,
                    UnreadCount = 0,
                    AvatarText = GetAvatarText(dialog.ChatName),
                    AvatarColor = avatarColors[random.Next(avatarColors.Length)]
                };

                chats.Insert(0, newChat);
                UpdateChatsList();
                SelectChat(newChat);
            }
        }

        private string GetAvatarText(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();

            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateChatsList();
            ClearSearchButton.Visibility = string.IsNullOrEmpty(SearchTextBox.Text) ?
                Visibility.Collapsed : Visibility.Visible;
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SearchTextBox.Focus();
        }

        private void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                e.Handled = true;
                SendMessage();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void AvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите аватар"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Загружаем изображение
                    var imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.UriSource = new Uri(openFileDialog.FileName);
                    imageSource.CacheOption = BitmapCacheOption.OnLoad;
                    imageSource.EndInit();

                    // Создаем новый элемент для отображения картинки вместо текста
                    var imageBorder = new Border
                    {
                        Width = 48,
                        Height = 48,
                        CornerRadius = new CornerRadius(24),
                        Background = new ImageBrush(imageSource)
                        {
                            Stretch = Stretch.UniformToFill
                        },
                        ClipToBounds = true
                    };

                    // Заменяем содержимое кнопки аватара
                    AvatarButton.Content = imageBorder;

                    // Сохраняем путь к аватару в локальной переменной
                    string avatarPath = openFileDialog.FileName;

                    // Можно сохранить в файл или просто использовать в текущей сессии
                    // Для сохранения между запусками можно использовать файл в AppData
                    SaveAvatarToFile(imageSource, avatarPath);

                    // Обновляем аватар во всех сообщениях пользователя
                    UpdateUserAvatar(imageSource);

                    MessageBox.Show("Аватар успешно изменен!",
                        "Аватар", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки аватара: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Сохранение аватара в файл для использования между запусками
        private void SaveAvatarToFile(BitmapImage image, string sourcePath)
        {
            try
            {
                // Создаем папку для аватаров в AppData, если её нет
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = System.IO.Path.Combine(appDataPath, "NikitaMessenger");
                string avatarFile = System.IO.Path.Combine(appFolder, "avatar.png");

                Directory.CreateDirectory(appFolder);

                // Копируем файл
                File.Copy(sourcePath, avatarFile, true);
            }
            catch (Exception ex)
            {
                // Не критичная ошибка, просто логируем
                Console.WriteLine($"Не удалось сохранить аватар: {ex.Message}");
            }
        }

        // Загрузка аватара при запуске приложения
        private void LoadAvatarOnStartup()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = System.IO.Path.Combine(appDataPath, "NikitaMessenger");
                string avatarFile = System.IO.Path.Combine(appFolder, "avatar.png");

                if (File.Exists(avatarFile))
                {
                    var imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.UriSource = new Uri(avatarFile);
                    imageSource.CacheOption = BitmapCacheOption.OnLoad;
                    imageSource.EndInit();

                    var imageBorder = new Border
                    {
                        Width = 48,
                        Height = 48,
                        CornerRadius = new CornerRadius(24),
                        Background = new ImageBrush(imageSource)
                        {
                            Stretch = Stretch.UniformToFill
                        },
                        ClipToBounds = true
                    };

                    AvatarButton.Content = imageBorder;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить аватар: {ex.Message}");
            }
        }

        private void UpdateUserAvatar(ImageSource newAvatar)
        {
            // В текущей реализации мы не можем сохранить ImageSource в модель сообщения,
            // так как модель использует Brush для цвета.
            // Можно либо изменить модель, либо просто обновить UI

            // Просто перерисовываем сообщения, если чат активен
            if (currentChat != null && ChatHeader.Visibility == Visibility.Visible)
            {
                ShowMessages(currentChat);
            }
        }

        private void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Все файлы (*.*)|*.*",
                Title = "Выберите файл для отправки"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                MessageBox.Show($"Файл выбран: {System.IO.Path.GetFileName(openFileDialog.FileName)}",
                    "Файл", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AttachImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                MessageBox.Show($"Изображение выбрано: {System.IO.Path.GetFileName(openFileDialog.FileName)}",
                    "Изображение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CallButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChat != null)
            {
                MessageBox.Show($"Звонок пользователю {currentChat.Name}",
                    "Звонок", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChat != null)
            {
                var menu = new ContextMenu();

                var clearHistoryItem = new MenuItem { Header = "Очистить историю" };
                clearHistoryItem.Click += (s, args) =>
                {
                    if (MessageBox.Show("Очистить историю переписки?", "Подтверждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        currentChat.Messages.Clear();
                        currentChat.LastMessage = "";
                        ShowMessages(currentChat);
                        UpdateChatsList();
                    }
                };

                var deleteChatItem = new MenuItem { Header = "Удалить чат" };
                deleteChatItem.Click += (s, args) =>
                {
                    if (MessageBox.Show("Удалить этот чат?", "Подтверждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        chats.Remove(currentChat);
                        currentChat = null;
                        UpdateChatsList();

                        // Скрываем панель чата
                        ChatHeader.Visibility = Visibility.Collapsed;
                        MessagesBorder.Visibility = Visibility.Collapsed;
                        MessageInputBorder.Visibility = Visibility.Collapsed;
                        NoChatSelectedBorder.Visibility = Visibility.Visible;
                    }
                };

                menu.Items.Add(clearHistoryItem);
                menu.Items.Add(new Separator());
                menu.Items.Add(deleteChatItem);

                menu.PlacementTarget = sender as Button;
                menu.IsOpen = true;
            }
        }

        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "" && textBox.Foreground.ToString() == "#FF999999")
            {
                textBox.Text = "";
                textBox.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            }
        }
    }
}