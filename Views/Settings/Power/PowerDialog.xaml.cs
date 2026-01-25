using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoOS.Views.Settings.Power
{
    public sealed partial class PowerDialog : Page
    {
        public PowerDialogState State { get; }

        public PowerDialog(PowerSetting setting)
        {
            InitializeComponent();
            State = new PowerDialogState(setting);
            DataContext = State;

            if (State.IsOption)
                LoadEnumValues(setting);
        }

        private void LoadEnumValues(PowerSetting setting)
        {
            uint index = 0;

            while (true)
            {
                string name = PowerApi.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, index);
                if (string.IsNullOrWhiteSpace(name))
                    break;

                State.EnumValues.Add(new PowerSettingValueInfo
                {
                    Index = index,
                    FriendlyName = name,
                    Description = PowerApi.ReadPossibleDescription(setting.SubgroupGuid, setting.Guid, index)
                });

                index++;
            }
        }

        public uint GetAcValue() => State.AcValue;
        public uint GetDcValue() => State.DcValue;
    }

    public sealed class PowerDialogState : INotifyPropertyChanged
    {
        private readonly uint _originalAc;
        private readonly uint _originalDc;

        public PowerSetting Setting { get; }

        public ObservableCollection<PowerSettingValueInfo> EnumValues { get; } = [];

        private uint _acValue;
        private uint _dcValue;

        public bool IsValue => Setting.Min.HasValue && Setting.Max.HasValue && Setting.Increment.HasValue && Setting.Max.Value > Setting.Min.Value && Setting.Increment.Value > 0;

        public bool IsOption => !IsValue;

        public uint AcValue
        {
            get => _acValue;
            set
            {
                if (_acValue != value)
                {
                    _acValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public uint DcValue
        {
            get => _dcValue;
            set
            {
                if (_dcValue != value)
                {
                    _dcValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ValueToolTip
        {
            get
            {
                if (!IsValue)
                    return null;

                return $"Range: {Setting.Min} - {Setting.Max}\n" + $"Increment: {Setting.Increment}\n" + $"Unit: {char.ToUpper(Setting.Unit[0]) + Setting.Unit[1..]}";
            }
        }

        public PowerDialogState(PowerSetting setting)
        {
            Setting = setting;
            _acValue = _originalAc = setting.AcValueIndex;
            _dcValue = _originalDc = setting.DcValueIndex;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
