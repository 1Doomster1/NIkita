using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NIkita
{
    public enum MessageType
    {
        Text,
        Image,
        File
    }

    public enum MessageStatus
    {
        Sent,
        Delivered,
        Read
    }

    public class Message : INotifyPropertyChanged
    {
        private string _id;
        private string _sender;
        private string _content;
        private DateTime _time;
        private bool _isMyMessage;
        private MessageType _type;
        private MessageStatus _status;
        private string _imagePath;

        public string Id
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged(); }
        }

        public string Sender
        {
            get { return _sender; }
            set { _sender = value; OnPropertyChanged(); }
        }

        public string Content
        {
            get { return _content; }
            set { _content = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp
        {
            get { return _time; }
            set { _time = value; OnPropertyChanged(); }
        }

        public bool IsMyMessage
        {
            get { return _isMyMessage; }
            set { _isMyMessage = value; OnPropertyChanged(); }
        }

        public MessageType Type
        {
            get { return _type; }
            set { _type = value; OnPropertyChanged(); }
        }

        public MessageStatus Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged(); }
        }

        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; OnPropertyChanged(); }
        }

        public string TimeString => Timestamp.ToString("HH:mm");
        public string DateString => Timestamp.ToString("dd.MM.yyyy");

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}