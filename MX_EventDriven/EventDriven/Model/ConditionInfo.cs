using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EventDriven.Model
{
    public class ConditionInfo : INotifyPropertyChanged
    {
        private bool _isMet;
        public bool IsMet
        {
            get { return _isMet; }
            set
            {
                if (_isMet != value)
                {
                    _isMet = value;
                    OnPropertyChanged();
                }
            }
        }       

        private string _action;
        public string Action
        {
            get { return _action; }
            set
            {
                if (_action != value)
                {
                    _action = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        private object _exceptedValue; // Using object to allow different types
        public object ExceptedValue
        {
            get { return _exceptedValue; }
            set
            {
                if (_exceptedValue != value)
                {
                    _exceptedValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private object _currentValue; // Using object to allow different types
        public object CurrentValue
        {
            get { return _currentValue; }
            set
            {
                if (_currentValue != value)
                {
                    _currentValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
