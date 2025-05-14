using System.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EventDriven.Model
{
    public class TriggerInfo : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isExecuting;
        public bool IsExecuting
        {
            get { return _isExecuting; }
            set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _currentStep;
        public int CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _totalSteps;
        public int TotalSteps
        {
            get { return _totalSteps; }
            set
            {
                if (_totalSteps != value)
                {
                    _totalSteps = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<ConditionInfo> _conditions;
        public ObservableCollection<ConditionInfo> Conditions
        {
            get { return _conditions; }
            set
            {
                if (_conditions != value)
                {
                    _conditions = value;
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
