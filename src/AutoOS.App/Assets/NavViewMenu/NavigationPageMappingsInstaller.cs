namespace AutoOS.Assets.NavViewMenu;
public partial class NavigationPageMappingsInstaller
{
    public static Dictionary<string, Type> PageDictionary { get; } = new Dictionary<string, Type>
    {
        {"AutoOS.Views.Installer.HomeLandingPage", typeof(AutoOS.Views.Installer.HomeLandingPage)},
        {"AutoOS.Views.Installer.PersonalizationPage", typeof(AutoOS.Views.Installer.PersonalizationPage)},
        {"AutoOS.Views.Installer.BrowsersPage", typeof(AutoOS.Views.Installer.BrowsersPage)},
        {"AutoOS.Views.Installer.ApplicationsPage", typeof(AutoOS.Views.Installer.ApplicationsPage)},
        {"AutoOS.Views.Installer.DisplayPage", typeof(AutoOS.Views.Installer.DisplayPage)},
        {"AutoOS.Views.Installer.GraphicsPage", typeof(AutoOS.Views.Installer.GraphicsPage)},
        {"AutoOS.Views.Installer.SecurityPage", typeof(AutoOS.Views.Installer.SecurityPage)},
        {"AutoOS.Views.Installer.InstallPage", typeof(AutoOS.Views.Installer.InstallPage)},
    };
}
