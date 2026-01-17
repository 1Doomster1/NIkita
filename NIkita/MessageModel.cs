using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace NikitaMicrosoft
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
        private string _filePath;


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
        [NotMapped]
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }
        [NotMapped]
        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; OnPropertyChanged(); }
        }
        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string ChatId { get; set; }

        // Свойства для привязки без конвертеров
        public string TimeString => Timestamp.ToString("HH:mm");
        public string DateString => Timestamp.ToString("dd.MM.yyyy");

        public string StatusIcon => Status switch
        {
            MessageStatus.Sent => "✓",
            MessageStatus.Delivered => "✓✓",
            MessageStatus.Read => "✓✓",
            _ => ""
        };

        public Brush MessageForeground => IsMyMessage ? Brushes.White : Brushes.Black;

        public bool IsTextType => Type == MessageType.Text;
        public bool IsFileType => Type == MessageType.File;
        public bool IsImageType => Type == MessageType.Image;

        public string FileSize
        {
            get
            {
                if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
                {
                    try
                    {
                        var info = new FileInfo(ImagePath);
                        long bytes = info.Length;
                        string[] sizes = { "B", "KB", "MB", "GB" };
                        int order = 0;
                        double len = bytes;

                        while (len >= 1024 && order < sizes.Length - 1)
                        {
                            order++;
                            len /= 1024;
                        }

                        return $"{len:0.#} {sizes[order]}";
                    }
                    catch
                    {
                        return "Неизвестно";
                    }
                }
                return "Неизвестно";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}