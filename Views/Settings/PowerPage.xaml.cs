using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using AutoOS.Views.Settings.Power;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Windows.System;

namespace AutoOS.Views.Settings
{
    public sealed partial class PowerPage : Page
    {
        private bool isInitializingPowerPlans = true;

        private readonly ObservableCollection<PowerPlan> _powerPlans = [];
        public ObservableCollection<PowerSubgroup> Subgroups { get; } = [];
        private ObservableCollection<PowerSubgroup> _allSubgroups = [];

        public PowerPage()
        {
            InitializeComponent();
            Loaded += PowerPage_Loaded;
        }

        private async void PowerPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPowerPlans();
            await LoadPowerPlanSettings(((PowerPlan)PowerPlanComboBox.SelectedItem).Guid);
            Search.IsEnabled = true;
            PowerPlanComboBox.IsEnabled = true;
            Import.IsEnabled = true;
            SwitchPresenter.Value = "Loaded";
            PowerTreeView.UpdateLayout();
        }

        private void LoadPowerPlans()
        {
            var plansList = new List<PowerPlan>();
            uint index = 0;

            while (true)
            {
                uint size = (uint)Marshal.SizeOf<Guid>();
                byte[] buffer = new byte[size];

                uint res = PowerApi.PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, PowerDataAccessor.AccessScheme, index++, buffer, ref size);
                if (res != 0) break;

                Guid schemeGuid = new(buffer);
                plansList.Add(new PowerPlan
                {
                    Guid = schemeGuid,
                    Name = PowerApi.ReadFriendlyName(schemeGuid, null, null)
                });
            }

            PowerApi.PowerGetActiveScheme(IntPtr.Zero, out var activePtr);
            Guid activeScheme = Marshal.PtrToStructure<Guid>(activePtr);
            PowerApi.LocalFree(activePtr);

            foreach (var plan in plansList.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
                _powerPlans.Add(plan);

            PowerPlanComboBox.ItemsSource = _powerPlans;
            PowerPlanComboBox.SelectedItem = _powerPlans.FirstOrDefault(p => p.Guid == activeScheme);

            isInitializingPowerPlans = false;
        }

        private async Task LoadPowerPlanSettings(Guid scheme)
        {
            await Task.Run(async () =>
            {
                IntPtr schemePtr = PowerApi.AllocGuid(scheme);
                var allSubgroupsList = new List<PowerSubgroup>();

                try
                {
                    Guid noneSubgroupGuid = new("fea3413e-7e05-4911-9a71-700331f1c294");
                    var noneSubgroup = new PowerSubgroup
                    {
                        Guid = noneSubgroupGuid,
                        Name = "None"
                    };

                    uint settingIndex = 0;
                    uint guidSize = (uint)Marshal.SizeOf<Guid>();

                    while (true)
                    {
                        byte[] setBuffer = new byte[guidSize];
                        uint size = guidSize;

                        uint res = PowerApi.PowerEnumerate(IntPtr.Zero, schemePtr, IntPtr.Zero, PowerDataAccessor.AccessIndividualSetting, settingIndex++, setBuffer, ref size);
                        if (res != 0) break;

                        Guid settingGuid = new(setBuffer);
                        noneSubgroup.Settings.Add(new PowerSetting
                        {
                            SubgroupGuid = noneSubgroupGuid,
                            Guid = settingGuid,
                            Name = PowerApi.ReadFriendlyName(scheme, noneSubgroupGuid, settingGuid),
                            Description = PowerApi.ReadDescription(scheme, noneSubgroupGuid, settingGuid),
                            AcValueIndex = PowerApi.ReadAcValueIndex(scheme, noneSubgroupGuid, settingGuid),
                            DcValueIndex = PowerApi.ReadDcValueIndex(scheme, noneSubgroupGuid, settingGuid),
                            Min = PowerApi.ReadValueMin(noneSubgroupGuid, settingGuid),
                            Max = PowerApi.ReadValueMax(noneSubgroupGuid, settingGuid),
                            Increment = PowerApi.ReadValueIncrement(noneSubgroupGuid, settingGuid),
                            Unit = PowerApi.ReadValueUnitsSpecifier(noneSubgroupGuid, settingGuid)
                        });
                    }

                    allSubgroupsList.Add(noneSubgroup);

                    uint subgroupIndex = 0;
                    while (true)
                    {
                        byte[] sgBuffer = new byte[guidSize];
                        uint size = guidSize;

                        uint res = PowerApi.PowerEnumerate(IntPtr.Zero, schemePtr, IntPtr.Zero, PowerDataAccessor.AccessSubgroup, subgroupIndex++, sgBuffer, ref size);
                        if (res != 0) break;

                        Guid subgroupGuid = new(sgBuffer);
                        PowerSubgroup subgroup = new()
                        {
                            Guid = subgroupGuid,
                            Name = subgroupGuid == new Guid("9596fb26-9850-41fd-ac3e-f7c3c00afd4b") ? "Multimedia settings" : PowerApi.ReadFriendlyName(scheme, subgroupGuid, null)
                        };

                        IntPtr subgroupPtr = PowerApi.AllocGuid(subgroupGuid);
                        try
                        {
                            uint settingIdx = 0;
                            while (true)
                            {
                                byte[] setBuffer = new byte[guidSize];
                                size = guidSize;

                                res = PowerApi.PowerEnumerate(IntPtr.Zero, schemePtr, subgroupPtr, PowerDataAccessor.AccessIndividualSetting, settingIdx++, setBuffer, ref size);
                                if (res != 0) break;

                                Guid settingGuid = new(setBuffer);
                                subgroup.Settings.Add(new PowerSetting
                                {
                                    SubgroupGuid = subgroupGuid,
                                    Guid = settingGuid,
                                    Name = PowerApi.ReadFriendlyName(scheme, subgroupGuid, settingGuid),
                                    Description = PowerApi.ReadDescription(scheme, subgroupGuid, settingGuid),
                                    AcValueIndex = PowerApi.ReadAcValueIndex(scheme, subgroupGuid, settingGuid),
                                    DcValueIndex = PowerApi.ReadDcValueIndex(scheme, subgroupGuid, settingGuid),
                                    Min = PowerApi.ReadValueMin(subgroupGuid, settingGuid),
                                    Max = PowerApi.ReadValueMax(subgroupGuid, settingGuid),
                                    Increment = PowerApi.ReadValueIncrement(subgroupGuid, settingGuid),
                                    Unit = PowerApi.ReadValueUnitsSpecifier(subgroupGuid, settingGuid)
                                });
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(subgroupPtr);
                        }

                        allSubgroupsList.Add(subgroup);
                    }

                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        _allSubgroups = [];

                        foreach (var sg in allSubgroupsList)
                        {
                            var sortedSettings = sg.Settings.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                            sg.Settings.Clear();
                            foreach (var setting in sortedSettings)
                                sg.Settings.Add(setting);

                            _allSubgroups.Add(sg);
                            Subgroups.Add(sg);
                        }
                    });
                }
                finally
                {
                    Marshal.FreeHGlobal(schemePtr);
                }
            });
        }

        private void PowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializingPowerPlans) return;

            if (PowerPlanComboBox.SelectedItem is PowerPlan selectedPlan)
            {
                var schemeGuid = selectedPlan.Guid;
                PowerApi.PowerSetActiveScheme(IntPtr.Zero, ref schemeGuid);

                foreach (var subgroup in _allSubgroups)
                {
                    foreach (var setting in subgroup.Settings)
                    {
                        setting.AcValueIndex = PowerApi.ReadAcValueIndex(schemeGuid, subgroup.Guid, setting.Guid);
                        setting.DcValueIndex = PowerApi.ReadDcValueIndex(schemeGuid, subgroup.Guid, setting.Guid);
                    }
                }
            }
        }

        private readonly PowerSubgroup noResultItem = new()
        {
            Name = "No result found",
            Settings = [],
            IsVisible = true
        };

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = Search.Text.Trim();
            bool anyVisible = false;

            foreach (var subgroup in _allSubgroups)
            {
                foreach (var setting in subgroup.Settings)
                {
                    setting.IsVisible = string.IsNullOrEmpty(query) || setting.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase);
                }

                subgroup.IsVisible = subgroup.Settings.Any(s => s.IsVisible);

                if (subgroup.IsVisible) anyVisible = true;
            }

            if (!anyVisible)
            {
                noResultItem.IsVisible = true;
                if (!Subgroups.Contains(noResultItem))
                    Subgroups.Add(noResultItem);
            }
            else
            {
                if (Subgroups.Contains(noResultItem))
                    Subgroups.Remove(noResultItem);
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FilePicker(App.MainWindow)
            {
                ShowAllFilesOption = false
            };
            picker.FileTypeChoices.Add("Power Scheme Files", ["*.pow"]);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
                return;

            uint res = PowerApi.PowerImportPowerScheme(IntPtr.Zero, file.Path, out var destSchemePtr);
            if (res != 0 || destSchemePtr == IntPtr.Zero)
                return;

            Guid importedGuid;
            try
            {
                importedGuid = Marshal.PtrToStructure<Guid>(destSchemePtr);
            }
            finally
            {
                PowerApi.LocalFree(destSchemePtr);
            }

            PowerApi.PowerSetActiveScheme(IntPtr.Zero, ref importedGuid);

            if (!_powerPlans.Any(p => p.Guid == importedGuid))
            {
                var plan = new PowerPlan
                {
                    Guid = importedGuid,
                    Name = PowerApi.ReadFriendlyName(importedGuid, null, null)
                };
                _powerPlans.Add(plan);
            }
            PowerPlanComboBox.SelectedItem = _powerPlans.First(p => p.Guid == importedGuid);

            foreach (var subgroup in _allSubgroups)
            {
                foreach (var setting in subgroup.Settings)
                {
                    setting.AcValueIndex = PowerApi.ReadAcValueIndex(importedGuid, subgroup.Guid, setting.Guid);
                    setting.DcValueIndex = PowerApi.ReadDcValueIndex(importedGuid, subgroup.Guid, setting.Guid);
                }
            }
        }

        private async void TreeView_ItemInvoked(object sender, TreeViewItemInvokedEventArgs args)
        {
            switch (args.InvokedItem)
            {
                case PowerSubgroup subgroup:
                    subgroup.IsExpanded = !subgroup.IsExpanded;
                    break;

                case PowerSetting setting:
                    PowerApi.PowerGetActiveScheme(IntPtr.Zero, out var activePtr);
                    Guid activeScheme = Marshal.PtrToStructure<Guid>(activePtr);
                    PowerApi.LocalFree(activePtr);

                    var dialog = new PowerDialog(setting);

                    var contentDialog = new ContentDialog
                    {
                        Content = dialog,
                        PrimaryButtonText = "Apply",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = XamlRoot,
                        Title = setting.Name
                    };
                    contentDialog.Resources["ContentDialogMaxWidth"] = 600;

                    var result = await contentDialog.ShowAsync();
                    if (result != ContentDialogResult.Primary) return;

                    uint newAcValue = dialog.GetAcValue();
                    uint newDcValue = dialog.GetDcValue();

                    IntPtr schemePtr = PowerApi.AllocGuid(activeScheme);
                    IntPtr subgroupPtr = PowerApi.AllocGuid(setting.SubgroupGuid);
                    IntPtr settingPtr = PowerApi.AllocGuid(setting.Guid);

                    try
                    {
                        PowerApi.PowerWriteACValueIndex(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, newAcValue);
                        PowerApi.PowerWriteDCValueIndex(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, newDcValue);
                        PowerApi.PowerSetActiveScheme(IntPtr.Zero, ref activeScheme);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(schemePtr);
                        Marshal.FreeHGlobal(subgroupPtr);
                        Marshal.FreeHGlobal(settingPtr);
                    }

                    setting.AcValueIndex = newAcValue;
                    setting.DcValueIndex = newDcValue;
                    break;
            }
        }
    }
}
