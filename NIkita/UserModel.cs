using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NikitaMessenger
{
    public class UserModel : INotifyPropertyChanged
    {
        private string _id;
        private string _username;
        private bool _isOnline;
        private string _avatarPath;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}