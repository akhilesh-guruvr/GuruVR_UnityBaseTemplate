using System;

namespace Presentation.Model
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnLoadingChanged?.Invoke(_isLoading);
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnStatusMessageChanged?.Invoke(_statusMessage);
            }
        }

        public event Action<bool> OnLoadingChanged;
        public event Action<string> OnStatusMessageChanged;
    }
}
