using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Text;

namespace AutoOS.Views.Settings.Power
{
    public sealed class PowerCompareSubgroup : INotifyPropertyChanged
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }

        public Windows.UI.Text.FontWeight FontWeight { get; set; } = FontWeights.SemiBold;

        public ObservableCollection<PowerCompareSetting> Settings { get; set; } = [];
        public ObservableCollection<object> SubItems => new(Settings.Cast<object>());

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

    public sealed class PowerCompareSetting : INotifyPropertyChanged
    {
        public Guid SubgroupGuid { get; set; }
        public Guid Guid { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public uint? Min { get; set; }
        public uint? Max { get; set; }
        public uint? Increment { get; set; }
        public string Unit { get; set; }

        public bool IsOption => !(Min.HasValue && Max.HasValue && Increment.HasValue && Max.Value > Min.Value && Increment.Value > 0);

        private uint _plan1AcValue;
        public uint Plan1AcValue
        {
            get => _plan1AcValue;
            set { if (_plan1AcValue != value) { _plan1AcValue = value; OnPropertyChanged(); } }
        }

        private uint _plan1DcValue;
        public uint Plan1DcValue
        {
            get => _plan1DcValue;
            set { if (_plan1DcValue != value) { _plan1DcValue = value; OnPropertyChanged(); } }
        }

        private uint _plan2AcValue;
        public uint Plan2AcValue
        {
            get => _plan2AcValue;
            set { if (_plan2AcValue != value) { _plan2AcValue = value; OnPropertyChanged(); } }
        }

        private uint _plan2DcValue;
        public uint Plan2DcValue
        {
            get => _plan2DcValue;
            set { if (_plan2DcValue != value) { _plan2DcValue = value; OnPropertyChanged(); } }
        }

        private string _plan1AcFriendlyValue;
        public string Plan1AcFriendlyValue
        {
            get => _plan1AcFriendlyValue;
            set { if (_plan1AcFriendlyValue != value) { _plan1AcFriendlyValue = value; OnPropertyChanged(); } }
        }

        private string _plan1DcFriendlyValue;
        public string Plan1DcFriendlyValue
        {
            get => _plan1DcFriendlyValue;
            set { if (_plan1DcFriendlyValue != value) { _plan1DcFriendlyValue = value; OnPropertyChanged(); } }
        }

        private string _plan2AcFriendlyValue;
        public string Plan2AcFriendlyValue
        {
            get => _plan2AcFriendlyValue;
            set { if (_plan2AcFriendlyValue != value) { _plan2AcFriendlyValue = value; OnPropertyChanged(); } }
        }

        private string _plan2DcFriendlyValue;
        public string Plan2DcFriendlyValue
        {
            get => _plan2DcFriendlyValue;
            set { if (_plan2DcFriendlyValue != value) { _plan2DcFriendlyValue = value; OnPropertyChanged(); } }
        }

        private bool _isAcDifferent = false;
        public bool IsAcDifferent
        {
            get => _isAcDifferent;
            set 
            { 
                if (_isAcDifferent != value) 
                { 
                    _isAcDifferent = value; 
                    OnPropertyChanged();
                } 
            }
        }

        private bool _isDcDifferent = false;
        public bool IsDcDifferent
        {
            get => _isDcDifferent;
            set 
            { 
                if (_isDcDifferent != value) 
                { 
                    _isDcDifferent = value; 
                    OnPropertyChanged();
                } 
            }
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

    public sealed class PowerCompareItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SubgroupTemplate { get; set; }
        public DataTemplate SettingTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
            => item switch
            {
                PowerCompareSubgroup => SubgroupTemplate,
                PowerCompareSetting => SettingTemplate,
                _ => base.SelectTemplateCore(item)
            };
    }
}


