using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NIkita
{
    public class Chat : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _lastMessage;
        private DateTime _lastMessageTime;
        private bool _isOnline;
        private int _unreadCount;

        public string Id
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public string LastMessage
        {
            get { return _lastMessage; }
            set { _lastMessage = value; OnPropertyChanged(); }
        }

        public DateTime LastMessageTime
        {
            get { return _lastMessageTime; }
            set { _lastMessageTime = value; OnPropertyChanged(); }
        }

        public bool IsOnline
        {
            get { return _isOnline; }
            set { _isOnline = value; OnPropertyChanged(); }
        }

        public int UnreadCount
        {
            get { return _unreadCount; }
            set { _unreadCount = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        // Свойства для привязки без конвертеров
        public string LastMessageTimeString => LastMessageTime.ToString("HH:mm");
        public string LastMessageDateString => LastMessageTime.ToString("dd.MM");

        public string OnlineStatus => IsOnline ? "online" : "offline";

        public string Initials
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    if (Name.Length >= 2)
                    {
                        return Name.Substring(0, 2).ToUpper();
                    }
                    return Name.ToUpper();
                }
                return "??";
            }
        }

        public bool HasUnreadMessages => UnreadCount > 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}