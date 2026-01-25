using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoOS.Views.Settings.Power
{
    public sealed class PowerPlan
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
    }

    public sealed class PowerSubgroup : INotifyPropertyChanged
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public List<PowerSetting> Settings { get; set; } = [];
        public List<object> SubItems => Settings.Cast<object>().ToList();

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class PowerSetting : INotifyPropertyChanged
    {
        private uint _acValueIndex;
        private uint _dcValueIndex;

        public Guid SubgroupGuid { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public uint AcValueIndex
        {
            get => _acValueIndex;
            set
            {
                if (_acValueIndex != value)
                {
                    _acValueIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public uint DcValueIndex
        {
            get => _dcValueIndex;
            set
            {
                if (_dcValueIndex != value)
                {
                    _dcValueIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public uint? Min { get; set; }
        public uint? Max { get; set; }
        public uint? Increment { get; set; }
        public string Unit { get; set; }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class PowerSettingValueInfo
    {
        public uint Index { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
    }

    public sealed class PowerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SubgroupTemplate { get; set; }
        public DataTemplate SettingTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
            => item switch
            {
                PowerSubgroup => SubgroupTemplate,
                PowerSetting => SettingTemplate,
                _ => base.SelectTemplateCore(item)
            };
    }
}
