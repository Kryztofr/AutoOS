using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoOS.Views.Settings.Power;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using AutoOS.Helpers.Picker;

namespace AutoOS.Views.Settings
{
    public sealed partial class PowerPage : Page
    {
        private bool isInitializingPowerPlans = true;

        private readonly ObservableCollection<PowerPlan> _powerPlans = [];
        private readonly ObservableCollection<PowerPlan> _comparePlans = [];
        private readonly ObservableCollection<PowerSubgroup> _allSubgroups = [];
        public ObservableCollection<PowerSubgroup> Subgroups { get; } = [];
        public ObservableCollection<PowerCompareSubgroup> CompareSubgroups { get; } = [];
        private PowerCompareSubgroup _identicalPlansPlaceholder;

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
            ComparePowerPlanComboBox.IsEnabled = true;
            Edit.IsEnabled = true;
            Delete.IsEnabled = true;
            Export.IsEnabled = true;
            Import.IsEnabled = true;
            SwitchPresenter.Value = "Loaded";
            ActiveTreeView.UpdateLayout();
            CompareTreeView.UpdateLayout();
            CompareTreeView.Opacity = 0;
            CompareTreeView.IsHitTestVisible = false;
        }

        private unsafe void LoadPowerPlans()
        {
            var plansList = new List<PowerPlan>();
            uint index = 0;

            while (true)
            {
                uint size = (uint)sizeof(Guid);
                byte[] buffer = new byte[size];

                uint res;
                {
                    res = (uint)PInvoke.PowerEnumerate(default, null, null, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index++, (Span<byte>)buffer, ref size);
                }
                if (res != 0) break;

                Guid schemeGuid = new(buffer);
                plansList.Add(new PowerPlan
                {
                    Guid = schemeGuid,
                    Name = PowerApi.ReadFriendlyName(schemeGuid, null, null),
                    Description = PowerApi.ReadDescription(schemeGuid)
                });
            }

            Guid* activePtr;
            PInvoke.PowerGetActiveScheme(default, out activePtr);
            Guid activeScheme = activePtr != null ? *activePtr : Guid.Empty;
            if (activePtr != null) PInvoke.LocalFree((HLOCAL)activePtr);

            foreach (var plan in plansList.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                _powerPlans.Add(plan);
                _comparePlans.Add(plan);
            }

            var selectPlanToCompare = new PowerPlan
            {
                Guid = Guid.Empty,
                Name = "Select plan to compare",
                Description = "Select a power plan to compare against the active plan."
            };

            _comparePlans.Insert(0, selectPlanToCompare);
            PowerPlanComboBox.ItemsSource = _powerPlans;
            PowerPlanComboBox.SelectedItem = _powerPlans.FirstOrDefault(p => p.Guid == activeScheme);
            ComparePowerPlanComboBox.ItemsSource = _comparePlans;
            ComparePowerPlanComboBox.SelectedItem = selectPlanToCompare;
            isInitializingPowerPlans = false;
        }

        private async Task LoadPowerPlanSettings(Guid scheme)
        {
            var result = await Task.Run(() =>
            {
                    var allSubgroupsList = new List<PowerSubgroup>();
                    var compareSubgroupsList = new List<PowerCompareSubgroup>();

                    try
                    {
                    Guid noneSubgroupGuid = new("fea3413e-7e05-4911-9a71-700331f1c294");
                    uint guidSize = (uint)Marshal.SizeOf<Guid>();
                    var noneSubgroup = new PowerSubgroup
                    {
                        Guid = noneSubgroupGuid,
                        Name = "None"
                    };

                    var noneCompareSubgroup = new PowerCompareSubgroup
                    {
                        Guid = noneSubgroupGuid,
                        Name = "None",
                        IsExpanded = true,
                        IsVisible = true
                    };

                    uint settingIndex = 0;

                    uint res;
                    while (true)
                    {
                        byte[] setBuffer = new byte[guidSize];
                        uint size = guidSize;

                        {
                            res = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, null, POWER_DATA_ACCESSOR.ACCESS_INDIVIDUAL_SETTING, settingIndex++, (Span<byte>)setBuffer, ref size);
                        }
                        if (res != 0) break;

                        Guid settingGuid = new(setBuffer);
                        var setting = new PowerSetting
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
                        };

                        var friendlyAc = setting.AcValueIndex.ToString();
                        var friendlyDc = setting.DcValueIndex.ToString();

                        if (setting.IsOption)
                        {
                            friendlyAc = PowerApi.ReadPossibleFriendlyName(noneSubgroupGuid, settingGuid, setting.AcValueIndex);
                            friendlyDc = PowerApi.ReadPossibleFriendlyName(noneSubgroupGuid, settingGuid, setting.DcValueIndex);
                        }

                        setting.FriendlyAcValue = friendlyAc;
                        setting.FriendlyDcValue = friendlyDc;

                        noneSubgroup.Settings.Add(setting);

                        var compareSetting = new PowerCompareSetting
                        {
                            SubgroupGuid = noneSubgroupGuid,
                            Guid = settingGuid,
                            Name = setting.Name,
                            Description = setting.Description,
                            Min = setting.Min,
                            Max = setting.Max,
                            Increment = setting.Increment,
                            Unit = setting.Unit,
                            Plan1AcValue = setting.AcValueIndex,
                            Plan1DcValue = setting.DcValueIndex,
                            Plan1AcFriendlyValue = friendlyAc,
                            Plan1DcFriendlyValue = friendlyDc,
                            Plan2AcValue = 0,
                            Plan2DcValue = 0,
                            Plan2AcFriendlyValue = "",
                            Plan2DcFriendlyValue = "",
                            IsAcDifferent = false,
                            IsDcDifferent = false,
                            IsVisible = true
                        };
                    }

                    allSubgroupsList.Add(noneSubgroup);
                    compareSubgroupsList.Add(noneCompareSubgroup);

                    uint subgroupIndex = 0;
                    while (true)
                    {
                        byte[] sgBuffer = new byte[guidSize];
                        uint size = guidSize;

                        {
                            res = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, null, POWER_DATA_ACCESSOR.ACCESS_SUBGROUP, subgroupIndex++, (Span<byte>)sgBuffer, ref size);
                        }
                        if (res != 0) break;

                        Guid subgroupGuid = new(sgBuffer);
                        PowerSubgroup subgroup = new()
                        {
                            Guid = subgroupGuid,
                            Name = subgroupGuid == new Guid("9596fb26-9850-41fd-ac3e-f7c3c00afd4b") ? "Multimedia settings" : PowerApi.ReadFriendlyName(scheme, subgroupGuid, null)
                        };

                        PowerCompareSubgroup compareSubgroup = new()
                        {
                            Guid = subgroupGuid,
                            Name = subgroup.Name,
                            IsExpanded = true,
                            IsVisible = true
                        };

                            try
                            {
                            uint settingIdx = 0;
                            while (true)
                            {
                                byte[] setBuffer = new byte[guidSize];
                                size = guidSize;

                                 {
                                     res = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, (Guid?)subgroupGuid, POWER_DATA_ACCESSOR.ACCESS_INDIVIDUAL_SETTING, settingIdx++, (Span<byte>)setBuffer, ref size);
                                 }
                                 if (res != 0) break;

                                Guid settingGuid = new(setBuffer);
                                var setting = new PowerSetting
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
                                };

                                var friendlyAc = setting.AcValueIndex.ToString();
                                var friendlyDc = setting.DcValueIndex.ToString();

                                if (setting.IsOption)
                                {
                                    friendlyAc = PowerApi.ReadPossibleFriendlyName(subgroupGuid, settingGuid, setting.AcValueIndex);
                                    friendlyDc = PowerApi.ReadPossibleFriendlyName(subgroupGuid, settingGuid, setting.DcValueIndex);
                                }

                                setting.FriendlyAcValue = friendlyAc;
                                setting.FriendlyDcValue = friendlyDc;

                                subgroup.Settings.Add(setting);

                                var compareSetting = new PowerCompareSetting
                                {
                                    SubgroupGuid = subgroupGuid,
                                    Guid = settingGuid,
                                    Name = setting.Name,
                                    Description = setting.Description,
                                    Min = setting.Min,
                                    Max = setting.Max,
                                    Increment = setting.Increment,
                                    Unit = setting.Unit,
                                    Plan1AcValue = setting.AcValueIndex,
                                    Plan1DcValue = setting.DcValueIndex,
                                    Plan1AcFriendlyValue = friendlyAc,
                                    Plan1DcFriendlyValue = friendlyDc,
                                    Plan2AcValue = 0,
                                    Plan2DcValue = 0,
                                    Plan2AcFriendlyValue = "",
                                    Plan2DcFriendlyValue = "",
                                    IsAcDifferent = false,
                                    IsDcDifferent = false,
                                    IsVisible = true
                                };
                                compareSubgroup.Settings.Add(compareSetting);
                            }
                        }
                        finally
                        {
                        }

                        allSubgroupsList.Add(subgroup);
                        compareSubgroupsList.Add(compareSubgroup);
                    }

                }
                finally
                {
                }

                return (allSubgroupsList, compareSubgroupsList);
            });
            var allSubgroupsList = result.allSubgroupsList;
            var compareSubgroupsList = result.compareSubgroupsList;

            _allSubgroups.Clear();
            Subgroups.Clear();
            CompareSubgroups.Clear();

            foreach (var sg in allSubgroupsList)
            {
                var sortedSettings = sg.Settings.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                sg.Settings.Clear();
                foreach (var setting in sortedSettings)
                {
                    sg.Settings.Add(setting);
                }

                _allSubgroups.Add(sg);
                Subgroups.Add(sg);
            }

            foreach (var csg in compareSubgroupsList)
            {
                var sortedSettings = csg.Settings.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                csg.Settings.Clear();
                foreach (var setting in sortedSettings)
                {
                    csg.Settings.Add(setting);
                }
                CompareSubgroups.Add(csg);
            }
        }

        private readonly PowerSubgroup noResultItem = new()
        {
            Name = "No result found",
            Settings = [],
            IsVisible = true,
            FontWeight = FontWeights.Normal
        };

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ComparePowerPlanComboBox.SelectedItem is PowerPlan comparePlan && comparePlan.Guid == Guid.Empty && CompareTreeView.Visibility == Visibility.Visible)
                CompareTreeView.Visibility = Visibility.Collapsed;

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
                if (!Subgroups.Contains(noResultItem))
                    Subgroups.Add(noResultItem);
            }
            else
            {
                Subgroups.Remove(noResultItem);
            }
        }

        private async void PowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializingPowerPlans) return;

            if (PowerPlanComboBox.SelectedItem is PowerPlan selectedPlan)
            {
                var schemeGuid = selectedPlan.Guid;
                PowerApi.PowerSetActiveScheme(schemeGuid);

                foreach (var subgroup in _allSubgroups)
                {
                    foreach (var setting in subgroup.Settings)
                    {
                        setting.AcValueIndex = PowerApi.ReadAcValueIndex(schemeGuid, subgroup.Guid, setting.Guid);
                        setting.DcValueIndex = PowerApi.ReadDcValueIndex(schemeGuid, subgroup.Guid, setting.Guid);

                        if (setting.IsOption)
                        {
                            setting.FriendlyAcValue = PowerApi.ReadPossibleFriendlyName(subgroup.Guid, setting.Guid, setting.AcValueIndex);
                            setting.FriendlyDcValue = PowerApi.ReadPossibleFriendlyName(subgroup.Guid, setting.Guid, setting.DcValueIndex);
                        }
                        else
                        {
                            setting.FriendlyAcValue = setting.AcValueIndex.ToString();
                            setting.FriendlyDcValue = setting.DcValueIndex.ToString();
                        }
                    }
                }

                if (ComparePowerPlanComboBox.SelectedItem is PowerPlan comparePlan && comparePlan.Guid != Guid.Empty)
                {
                    await UpdateCompareData(comparePlan);
                    ActiveTreeView.Visibility = Visibility.Collapsed;
                    ActiveTreeView.Opacity = 0;
                    ActiveTreeView.IsHitTestVisible = false;
                    CompareTreeView.Opacity = 1;
                    CompareTreeView.IsHitTestVisible = true;
                }
                else
                {
                    ActiveTreeView.Visibility = Visibility.Visible;
                    ActiveTreeView.Opacity = 1;
                    ActiveTreeView.IsHitTestVisible = true;
                    CompareTreeView.Opacity = 0;
                    CompareTreeView.IsHitTestVisible = false;
                }
            }
        }

        private async void ComparePowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializingPowerPlans) return;

            if (ComparePowerPlanComboBox.SelectedItem is PowerPlan comparePlan && comparePlan.Guid != Guid.Empty)
            {
                await UpdateCompareData(comparePlan);
                ActiveTreeView.Visibility = Visibility.Collapsed;
                ActiveTreeView.Opacity = 0;
                ActiveTreeView.IsHitTestVisible = false;
                CompareTreeView.Opacity = 1;
                CompareTreeView.IsHitTestVisible = true;
                CompareTreeView.Visibility = Visibility.Visible;
            }
            else
            {
                ActiveTreeView.Visibility = Visibility.Visible;
                ActiveTreeView.Opacity = 1;
                ActiveTreeView.IsHitTestVisible = true;
                CompareTreeView.Opacity = 0;
                CompareTreeView.IsHitTestVisible = false;
                CompareTreeView.Visibility = Visibility.Collapsed;
            }
        }

        private async Task UpdateCompareData(PowerPlan comparePlan)
        {
            if (comparePlan == null || comparePlan.Guid == Guid.Empty)
            {
                foreach (var sg in CompareSubgroups)
                {
                    sg.IsVisible = true;
                    foreach (var setting in sg.Settings)
                        setting.IsVisible = true;
                }

                if (_identicalPlansPlaceholder != null && CompareSubgroups.Contains(_identicalPlansPlaceholder))
                    CompareSubgroups.Remove(_identicalPlansPlaceholder);

                return;
            }

            var activePlan = PowerPlanComboBox.SelectedItem as PowerPlan;
            if (activePlan == null)
                return;

            foreach (var sg in CompareSubgroups)
            {
                if (sg == _identicalPlansPlaceholder) continue;
                foreach (var setting in sg.Settings)
                {
                    uint p1Ac = PowerApi.ReadAcValueIndex(activePlan.Guid, setting.SubgroupGuid, setting.Guid);
                    uint p1Dc = PowerApi.ReadDcValueIndex(activePlan.Guid, setting.SubgroupGuid, setting.Guid);
                    uint p2Ac = PowerApi.ReadAcValueIndex(comparePlan.Guid, setting.SubgroupGuid, setting.Guid);
                    uint p2Dc = PowerApi.ReadDcValueIndex(comparePlan.Guid, setting.SubgroupGuid, setting.Guid);

                    setting.Plan1AcFriendlyValue = setting.IsOption ? PowerApi.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p1Ac) : p1Ac.ToString();
                    setting.Plan1DcFriendlyValue = setting.IsOption ? PowerApi.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p1Dc) : p1Dc.ToString();
                    setting.Plan2AcFriendlyValue = setting.IsOption ? PowerApi.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p2Ac) : p2Ac.ToString();
                    setting.Plan2DcFriendlyValue = setting.IsOption ? PowerApi.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p2Dc) : p2Dc.ToString();

                    setting.Plan1AcValue = p1Ac;
                    setting.Plan1DcValue = p1Dc;
                    setting.Plan2AcValue = p2Ac;
                    setting.Plan2DcValue = p2Dc;

                    setting.IsAcDifferent = p1Ac != p2Ac;
                    setting.IsDcDifferent = p1Dc != p2Dc;
                    setting.IsVisible = setting.IsAcDifferent || setting.IsDcDifferent;

                }
            }

            if (_identicalPlansPlaceholder == null)
            {
                _identicalPlansPlaceholder = new PowerCompareSubgroup
                {
                    Name = "Power plans are identical",
                    FontWeight = FontWeights.Normal,
                    IsVisible = true
                };
            }

            bool anyDifferent = false;

            foreach (var sg in CompareSubgroups)
            {
                if (sg == _identicalPlansPlaceholder) continue;
                sg.IsVisible = sg.Settings.Any(s => s.IsVisible);
                if (sg.IsVisible) anyDifferent = true;
            }

            if (!anyDifferent)
            {
                _identicalPlansPlaceholder.IsVisible = true;
                if (!CompareSubgroups.Contains(_identicalPlansPlaceholder))
                    CompareSubgroups.Add(_identicalPlansPlaceholder);
            }
            else
            {
                if (CompareSubgroups.Contains(_identicalPlansPlaceholder))
                    CompareSubgroups.Remove(_identicalPlansPlaceholder);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (PowerPlanComboBox.SelectedItem is not PowerPlan plan)
                return;

            var nameTextBox = new Microsoft.UI.Xaml.Controls.TextBox
            {
                Text = plan.Name,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var descriptionBox = new DevWinUI.TextBox
            {
                AcceptsReturn = true,
                Text = plan.Description
            };

            var panel = new StackPanel
            {
                Spacing = 4
            };

            panel.Children.Add(new TextBlock { Text = "Name:" });
            panel.Children.Add(nameTextBox);
            panel.Children.Add(new TextBlock { Text = "Description:" });
            panel.Children.Add(descriptionBox);

            var dialog = new ContentDialog
            {
                Title = "Edit Power Plan",
                Content = panel,
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            PowerApi.WriteSchemeFriendlyName(plan.Guid, nameTextBox.Text);
            PowerApi.WriteSchemeDescription(plan.Guid, descriptionBox.Text);

            plan.Name = nameTextBox.Text;
            plan.Description = descriptionBox.Text;

            var sortedPlans = _powerPlans.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            _powerPlans.Clear();
            foreach (var p in sortedPlans) _powerPlans.Add(p);

            var comparePlaceholder = _comparePlans.FirstOrDefault(p => p.Guid == Guid.Empty);
            var sortedCompare = _comparePlans.Where(p => p.Guid != Guid.Empty).OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            _comparePlans.Clear();
            if (comparePlaceholder != null) _comparePlans.Add(comparePlaceholder);
            foreach (var p in sortedCompare) _comparePlans.Add(p);

            var selected = PowerPlanComboBox.SelectedItem;
            PowerPlanComboBox.ItemsSource = null;
            PowerPlanComboBox.ItemsSource = _powerPlans;
            PowerPlanComboBox.SelectedItem = selected;

            var selectedCompare = ComparePowerPlanComboBox.SelectedItem;
            ComparePowerPlanComboBox.ItemsSource = null;
            ComparePowerPlanComboBox.ItemsSource = _comparePlans;
            ComparePowerPlanComboBox.SelectedItem = selectedCompare;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (PowerPlanComboBox.SelectedItem is not PowerPlan plan)
                return;

            if (_powerPlans.Count <= 1)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Unable to delete power plan",
                    Content = "At least one other power plan must exist.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Delete power plan",
                Content = $"Are you sure that you want to delete \"{plan.Name}\"?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            int currentIndex = _powerPlans.IndexOf(plan);

            PowerPlan nextSelection;
            if (currentIndex < _powerPlans.Count - 1)
                nextSelection = _powerPlans[currentIndex + 1];
            else
                nextSelection = _powerPlans[currentIndex - 1];

            PowerPlanComboBox.SelectedItem = nextSelection;

            PowerApi.DeleteScheme(plan.Guid);

            _powerPlans.Remove(plan);
            _comparePlans.Remove(_comparePlans.FirstOrDefault(p => p.Guid == plan.Guid));

            PowerPlanComboBox.SelectedItem = nextSelection;
            ComparePowerPlanComboBox.SelectedItem = _comparePlans.First(p => p.Guid == Guid.Empty);
        }

        private static unsafe Guid ImportPowerSchemeUnsafe(string filePath)
        {
            Guid* destSchemePtr = null;
            uint res = (uint)PInvoke.PowerImportPowerScheme(default, filePath, ref destSchemePtr);
            if (res != 0 || destSchemePtr == null)
                return Guid.Empty;

            try
            {
                return *destSchemePtr;
            }
            finally
            {
                PInvoke.LocalFree((HLOCAL)destSchemePtr);
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

            Guid importedGuid = ImportPowerSchemeUnsafe(file.Path);
            if (importedGuid == Guid.Empty)
                return;

            PowerApi.PowerSetActiveScheme(importedGuid);

            var plan = new PowerPlan
            {
                Guid = importedGuid,
                Name = PowerApi.ReadFriendlyName(importedGuid, null, null),
                Description = PowerApi.ReadDescription(importedGuid)
            };

            _powerPlans.Add(plan);
            
            var sortedPlans = _powerPlans.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            _powerPlans.Clear();
            foreach (var p in sortedPlans) _powerPlans.Add(p);

            _comparePlans.Add(plan);
            var comparePlaceholder = _comparePlans.FirstOrDefault(p => p.Guid == Guid.Empty);
            var sortedCompare = _comparePlans.Where(p => p.Guid != Guid.Empty).OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            _comparePlans.Clear();
            if (comparePlaceholder != null) _comparePlans.Add(comparePlaceholder);
            foreach (var p in sortedCompare) _comparePlans.Add(p);

            PowerPlanComboBox.ItemsSource = null;
            PowerPlanComboBox.ItemsSource = _powerPlans;
            PowerPlanComboBox.SelectedItem = _powerPlans.First(p => p.Guid == importedGuid);
            
            ComparePowerPlanComboBox.ItemsSource = null;
            ComparePowerPlanComboBox.ItemsSource = _comparePlans;
            ComparePowerPlanComboBox.SelectedItem = _comparePlans.First(p => p.Guid == Guid.Empty);

            foreach (var subgroup in _allSubgroups)
            {
                foreach (var setting in subgroup.Settings)
                {
                    setting.AcValueIndex = PowerApi.ReadAcValueIndex(importedGuid, subgroup.Guid, setting.Guid);
                    setting.DcValueIndex = PowerApi.ReadDcValueIndex(importedGuid, subgroup.Guid, setting.Guid);
                }
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (PowerPlanComboBox.SelectedItem is not PowerPlan plan)
                return;

            var picker = new SavePicker(App.MainWindow)
            {
                ShowAllFilesOption = false,
                SuggestedFileName = plan.Name
            };
            picker.FileTypeChoices.Add("Power Scheme Files", ["*.pow"]);

            var file = await picker.PickSaveFileAsync();
            if (file == null)
                return;

            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = @$"-export ""{file.Path}.pow"" {plan.Guid:D}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
                await proc.WaitForExitAsync();
        }

        private async void TreeView_ItemInvoked(object sender, TreeViewItemInvokedEventArgs args)
        {
            switch (args.InvokedItem)
            {
                case PowerSubgroup subgroup:
                    subgroup.IsExpanded = !subgroup.IsExpanded;
                    break;

                case PowerSetting setting:
                    var activePlan = PowerPlanComboBox.SelectedItem as PowerPlan;
                    Guid activeScheme = activePlan.Guid;

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

                    PowerApi.WriteACValueIndex(activeScheme, setting.SubgroupGuid, setting.Guid, newAcValue);
                    PowerApi.WriteDCValueIndex(activeScheme, setting.SubgroupGuid, setting.Guid, newDcValue);
                    PowerApi.PowerSetActiveScheme(activeScheme);

                    setting.AcValueIndex = newAcValue;
                    setting.DcValueIndex = newDcValue;
                    break;
            }
        }

        private async void CompareTreeView_ItemInvoked(object sender, TreeViewItemInvokedEventArgs args)
        {
            switch (args.InvokedItem)
            {
                case PowerCompareSubgroup subgroup:
                    subgroup.IsExpanded = !subgroup.IsExpanded;
                    break;

                case PowerCompareSetting compareSettings:
                    var activePlan = PowerPlanComboBox.SelectedItem as PowerPlan;
                    Guid activeScheme = activePlan.Guid;

                    var proxySetting = new PowerSetting
                    {
                        SubgroupGuid = compareSettings.SubgroupGuid,
                        Guid = compareSettings.Guid,
                        Name = compareSettings.Name,
                        Description = compareSettings.Description,
                        Min = compareSettings.Min,
                        Max = compareSettings.Max,
                        Increment = compareSettings.Increment,
                        Unit = compareSettings.Unit,
                        AcValueIndex = compareSettings.Plan1AcValue,
                        DcValueIndex = compareSettings.Plan1DcValue
                    };

                    var dialog = new PowerDialog(proxySetting);

                    var contentDialog = new ContentDialog
                    {
                        Content = dialog,
                        PrimaryButtonText = "Apply",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = XamlRoot,
                        Title = compareSettings.Name
                    };
                    contentDialog.Resources["ContentDialogMaxWidth"] = 600;

                    var result = await contentDialog.ShowAsync();
                    if (result != ContentDialogResult.Primary) return;

                    uint newAcValue = dialog.GetAcValue();
                    uint newDcValue = dialog.GetDcValue();

                    PowerApi.WriteACValueIndex(activeScheme, compareSettings.SubgroupGuid, compareSettings.Guid, newAcValue);
                    PowerApi.WriteDCValueIndex(activeScheme, compareSettings.SubgroupGuid, compareSettings.Guid, newDcValue);
                    PowerApi.PowerSetActiveScheme(activeScheme);

                    compareSettings.Plan1AcValue = newAcValue;
                    compareSettings.Plan1DcValue = newDcValue;

                    compareSettings.Plan1AcFriendlyValue = compareSettings.IsOption ? PowerApi.ReadPossibleFriendlyName(compareSettings.SubgroupGuid, compareSettings.Guid, newAcValue) : newAcValue.ToString();
                    compareSettings.Plan1DcFriendlyValue = compareSettings.IsOption ? PowerApi.ReadPossibleFriendlyName(compareSettings.SubgroupGuid, compareSettings.Guid, newDcValue) : newDcValue.ToString();

                    compareSettings.IsAcDifferent = compareSettings.Plan1AcValue != compareSettings.Plan2AcValue;
                    compareSettings.IsDcDifferent = compareSettings.Plan1DcValue != compareSettings.Plan2DcValue;
                    compareSettings.IsVisible = compareSettings.IsAcDifferent || compareSettings.IsDcDifferent;

                    var settingsSubgroup = _allSubgroups.FirstOrDefault(sg => sg.Guid == compareSettings.SubgroupGuid);
                    var settings = settingsSubgroup?.Settings.FirstOrDefault(s => s.Guid == compareSettings.Guid);

                    if (settings != null)
                    {
                        settings.AcValueIndex = newAcValue;
                        settings.DcValueIndex = newDcValue;
                        settings.FriendlyAcValue = compareSettings.Plan1AcFriendlyValue;
                        settings.FriendlyDcValue = compareSettings.Plan1DcFriendlyValue;
                    }

                    if (_identicalPlansPlaceholder == null)
                    {
                        _identicalPlansPlaceholder = new PowerCompareSubgroup
                        {
                            Name = "Power plans are identical",
                            FontWeight = FontWeights.Normal,
                            IsVisible = true
                        };
                    }

                    bool anyDifferent = false;
                    foreach (var sg in CompareSubgroups)
                    {
                        if (sg == _identicalPlansPlaceholder) continue;
                        sg.IsVisible = sg.Settings.Any(s => s.IsVisible);
                        if (sg.IsVisible) anyDifferent = true;
                    }

                    if (!anyDifferent)
                    {
                        _identicalPlansPlaceholder.IsVisible = true;
                        if (!CompareSubgroups.Contains(_identicalPlansPlaceholder))
                            CompareSubgroups.Add(_identicalPlansPlaceholder);
                    }
                    else
                    {
                        if (CompareSubgroups.Contains(_identicalPlansPlaceholder))
                            CompareSubgroups.Remove(_identicalPlansPlaceholder);
                    }
                    break;
            }
        }
    }
}
