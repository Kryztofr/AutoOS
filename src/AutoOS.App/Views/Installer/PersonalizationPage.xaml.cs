using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace AutoOS.Views.Installer;

public sealed partial class PersonalizationPage : Page
{
    private bool isInitializingThemeState = true;
    private bool isInitializingSchedule = true;
    private bool isInitializingContextMenuState = true;
    private bool isInitializingTrayIconsState = true;
    private bool isInitializingTaskbarAlignmentState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    private static readonly Guid CLSID_IThemeManager = new("C04B329E-5823-4415-9C93-BA44688947B0");
    private static readonly Guid IID_IThemeManager = new("0646EBBE-C1B7-4045-8FD0-FFD65D3FC792");

    private const uint CLSCTX_INPROC_SERVER = 1;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private static readonly IntPtr HWND_BROADCAST = new(0xffff);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int ApplyThemeFunc(IntPtr pThis, [MarshalAs(UnmanagedType.BStr)] string themePath);
    public PersonalizationPage()
    {
        InitializeComponent();
        GetItems();
        GetTheme();
        _ = GetSchedule();
        GetContextMenuState();
        GetTaskbarAlignmentState();
        GetTrayIconsState();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(PersonalizationPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    public unsafe static Task ApplyTheme(string themePath)
    {
        return Task.Run(() =>
        {
            var thread = new Thread(() =>
            {
                Guid clsid = CLSID_IThemeManager;
                Guid iid = IID_IThemeManager;

                void* pThemeManager;
                HRESULT hr = PInvoke.CoCreateInstance(
                    in clsid,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    in iid,
                    out pThemeManager);

                if (hr.Failed || pThemeManager == null) return;

                IntPtr handle = (IntPtr)pThemeManager;
                IntPtr vtable = Marshal.ReadIntPtr(handle);
                IntPtr applyThemePtr = Marshal.ReadIntPtr(vtable, IntPtr.Size * 4);

                var applyTheme = Marshal.GetDelegateForFunctionPointer<ApplyThemeFunc>(applyThemePtr);
                applyTheme(handle, themePath);

                fixed (char* pMessage = "ImmersiveColorSet")
                {
                    nuint result;
                    PInvoke.SendMessageTimeout(
                        (HWND)(nint)0xffff,
                        PInvoke.WM_SETTINGCHANGE,
                        new WPARAM(0),
                        new LPARAM((nint)pMessage),
                        SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG,
                        100,
                        &result);
                }

                Marshal.Release(handle);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        });
    }

    private void GetItems()
    {
        Themes.ItemsSource = new List<ThemeItem>
        {
            new() { LightTheme = @"C:\Windows\Web\Wallpaper\Windows\img0.jpg", DarkTheme = @"C:\Windows\Web\Wallpaper\Windows\img19.jpg" }
        };
    }

    private void GetTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes");
        string currentTheme = key?.GetValue("CurrentTheme") as string ?? string.Empty;

        if (currentTheme == @"C:\Windows\resources\Themes\aero.theme" || currentTheme == @"C:\Windows\resources\Themes\dark.theme")
        {
            Themes.SelectedIndex = 0;
        }

        isInitializingThemeState = false;
    }

    private void Theme_Changed(object sender, RoutedEventArgs e)
    {
        if (isInitializingThemeState) return;
    }

    private async Task UpdateTheme()
    {
        var now = DateTime.Now.TimeOfDay;
        bool shouldBeLight;

        if (TimeLine.StartTime <= TimeLine.EndTime)
        {
            shouldBeLight = now >= TimeLine.StartTime && now <= TimeLine.EndTime;
        }
        else
        {
            shouldBeLight = now >= TimeLine.StartTime || now <= TimeLine.EndTime;
        }
        bool currentlyLight = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("SystemUsesLightTheme") is int value && value != 0;

        if (shouldBeLight && !currentlyLight)
            await ApplyTheme(@"C:\Windows\Resources\Themes\aero.theme");
        else if (!shouldBeLight && currentlyLight)
            await ApplyTheme(@"C:\Windows\Resources\Themes\dark.theme");
    }

    private async Task GetSchedule()
    {
        try
        {
            string scheduleMode = localSettings.Values["ScheduleMode"] as string ?? "Sunset to sunrise";
            localSettings.Values["ScheduleMode"] = scheduleMode;

            ScheduleMode.SelectedIndex = scheduleMode switch
            {
                "Always Light" => 0,
                "Always Dark" => 1,
                "Sunset to sunrise" => 2,
                "Custom" => 3,
                _ => 2
            };

            // load custom
            LightTime.Time = (localSettings.Values["LightTime"] is string lightTimeStr && TimeSpan.TryParse(lightTimeStr, out var lt)) ? lt : TimeSpan.Parse("07:00");
            localSettings.Values["LightTime"] = LightTime.Time.ToString(@"hh\:mm");

            DarkTime.Time = (localSettings.Values["DarkTime"] is string darkTimeStr && TimeSpan.TryParse(darkTimeStr, out var dt)) ? dt : TimeSpan.Parse("19:00");
            localSettings.Values["DarkTime"] = DarkTime.Time.ToString(@"hh\:mm");

            // calculate sunrise sunset
            var pos = await LocationHelper.GetGeoLocationAsync();
            var sunTimes = SunTimesHelper.CalculateSunriseSunset(pos.Coordinate.Point.Position.Latitude, pos.Coordinate.Point.Position.Longitude, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            TimeLine.Sunrise = new TimeSpan(sunTimes.SunriseHour, sunTimes.SunriseMinute, 0);
            TimeLine.Sunset = new TimeSpan(sunTimes.SunsetHour, sunTimes.SunsetMinute, 0);

            // set timeline
            if (scheduleMode == "Sunset to sunrise")
            {
                TimeLine.StartTime = new TimeSpan(sunTimes.SunriseHour, sunTimes.SunriseMinute, 0);
                TimeLine.EndTime = new TimeSpan(sunTimes.SunsetHour, sunTimes.SunsetMinute, 0);
            }
            else if (scheduleMode == "Custom")
            {
                TimeLine.StartTime = LightTime.Time;
                TimeLine.EndTime = DarkTime.Time;
            }

            UpdateTimeCardsVisibility();
            await UpdateTheme();
            isInitializingSchedule = false;
        }
        catch
        {
            localSettings.Values["ScheduleMode"] = "Custom";
            ScheduleMode.SelectedIndex = 3;
            TimeLine.StartTime = LightTime.Time;
            TimeLine.EndTime = DarkTime.Time;
            UpdateTimeCardsVisibility();
            await UpdateTheme();
            isInitializingSchedule = false;
        }
    }

    private async void ScheduleMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingSchedule) return;

        string selected = (ScheduleMode.SelectedItem as ComboBoxItem)?.Content as string;
        localSettings.Values["ScheduleMode"] = selected;

        UpdateTimeCardsVisibility();

        if (selected == "Always Light")
            await ApplyTheme(@"C:\Windows\Resources\Themes\aero.theme");
        else if (selected == "Always Dark")
            await ApplyTheme(@"C:\Windows\Resources\Themes\dark.theme");
        else
            await GetSchedule();
    }

    private async void LightMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (isInitializingSchedule) return;

        localSettings.Values["LightTime"] = e.NewTime.ToString(@"hh\:mm");
        TimeLine.StartTime = e.NewTime;
        await UpdateTheme();
    }

    private async void DarkMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (isInitializingSchedule) return;

        localSettings.Values["DarkTime"] = e.NewTime.ToString(@"hh\:mm");
        TimeLine.EndTime = e.NewTime;
        await UpdateTheme();
    }

    private void UpdateTimeCardsVisibility()
    {
        var mode = (ScheduleMode.SelectedItem as ComboBoxItem)?.Content as string;

        LightTimeCard.Visibility = mode == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        DarkTimeCard.Visibility = mode == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        TimelineCard.Visibility = (mode == "Custom" || mode == "Sunset to sunrise") ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GetContextMenuState()
    {        
        ContextMenu.IsOn = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32") != null;

        isInitializingContextMenuState = false;
    }

    private async void ContextMenu_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingContextMenuState) return;

        if (ContextMenu.IsOn)
        {
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "", RegistryValueKind.String);
        }
        else
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false);
        }
    }

    private void GetTrayIconsState()
    {
        if (!localSettings.Values.TryGetValue("AlwaysShowTrayIcons", out object value))
        {
            localSettings.Values["AlwaysShowTrayIcons"] = 1;
            TrayIcons.IsChecked = true;
        }
        else
        {
            TrayIcons.IsChecked = Convert.ToInt32(value) == 1;
        }

        isInitializingTrayIconsState = false;
    }

    private void TrayIcons_Click(object sender, RoutedEventArgs e)
    {
        if (isInitializingTrayIconsState) return;

        localSettings.Values["AlwaysShowTrayIcons"] = (TrayIcons.IsChecked ?? false) ? 1 : 0;
    }

    private void GetTaskbarAlignmentState()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        var obj = key?.GetValue("TaskbarAl");
        int alignment = obj is int i && (i == 0 || i == 1) ? i : 1;

        TaskbarAlignment.SelectedIndex = alignment;
        TaskbarIcon.HeaderIcon = alignment == 0 ? new SymbolIcon(Symbol.AlignLeft) : new SymbolIcon(Symbol.AlignCenter);

        isInitializingTaskbarAlignmentState = false;
    }

    private async void TaskbarAlignment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingTaskbarAlignmentState) return;

        string value = TaskbarAlignment.SelectedIndex == 0 ? "0" : "1";
        Symbol icon = TaskbarAlignment.SelectedIndex == 0 ? Symbol.AlignLeft : Symbol.AlignCenter;

        Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", Convert.ToInt32(value), RegistryValueKind.DWord);

        TaskbarIcon.HeaderIcon = new SymbolIcon(icon);
    }
}

[GeneratedBindableCustomProperty]
public partial class ThemeItem
{
    public string LightTheme { get; set; }
    public string DarkTheme { get; set; }
}