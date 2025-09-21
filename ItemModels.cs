using System;

namespace timerthing.ItemModels
{

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