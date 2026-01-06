using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NIkita;

namespace NikitaMicrosoft
{
    public class MessageStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageStatus status)
            {
                return status switch
                {
                    MessageStatus.Sent => "✓",
                    MessageStatus.Delivered => "✓✓",
                    MessageStatus.Read => "✓✓",
                    _ => ""
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is int i)
            {
                return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType type && parameter is string param)
            {
                if (param == "Text" && type == MessageType.Text) return Visibility.Visible;
                if (param == "File" && type == MessageType.File) return Visibility.Visible;
                if (param == "Image" && type == MessageType.Image) return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrEmpty(name))
            {
                if (name.Length >= 2)
                {
                    return name.Substring(0, 2).ToUpper();
                }
                return name.ToUpper();
            }
            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && File.Exists(path))
            {
                try
                {
                    var info = new FileInfo(path);
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OnlineStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? "online" : "offline";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageStyleConverter : IValueConverter
    {
        public Style MyMessageStyle { get; set; }
        public Style OtherMessageStyle { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                return isMyMessage ? MyMessageStyle : OtherMessageStyle;
            }
            return OtherMessageStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                return isMyMessage ? Brushes.White : Brushes.Black;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                // Для своих сообщений - белый текст, для чужих - темный
                return isMyMessage ? Brushes.White : Brushes.Black;
            }

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                return isMyMessage ?
                    new SolidColorBrush(Color.FromRgb(0, 116, 204)) :
                    new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }

            return new SolidColorBrush(Color.FromRgb(245, 245, 245));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}