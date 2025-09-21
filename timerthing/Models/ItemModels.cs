using System;
using System.ComponentModel;

namespace timerthing.ItemModels
{
    public class ButtonInfo : INotifyPropertyChanged
    {
        private bool _isActive = false;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ItemGroup : ButtonInfo
    {
        public string Id { get; set; } = new Guid().ToString();

        private string _displayName = "New Group";
        public required string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }
    }

    public class ItemInfo : ButtonInfo
    {
        private string name = string.Empty;

        public required string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public required int Seconds { get; set; }
        public required int TotalSeconds { get; set; }

        public int ResetCount { get; set; } = 0;
        public int AverageResetTime { get; private set; } = 0;

        public string GUID { get; private set; } = Guid.NewGuid().ToString();

        // "0" is default group
        private string _group = "0";
        public string Group
        {
            get => _group;
            set
            {
                if (_group != value)
                {
                    _group = value;
                    OnPropertyChanged(nameof(Group));
                }
            }
        }

        public void OnReset()
        {
            ResetCount++;
            AverageResetTime = ResetCount > 0 ? TotalSeconds / ResetCount : 0;
        }
    }
}