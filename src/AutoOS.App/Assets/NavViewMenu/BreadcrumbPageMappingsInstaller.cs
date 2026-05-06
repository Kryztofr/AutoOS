using DevWinUI;

namespace AutoOS.Assets.NavViewMenu;

public partial class BreadcrumbPageMappingsInstaller
{
    public static Dictionary<Type, BreadcrumbPageConfig> PageDictionary { get; } = new()
    {
        { typeof(Views.Installer.HomeLandingPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
        { typeof(Views.Installer.PersonalizationPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
        { typeof(Views.Installer.BrowsersPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
        { typeof(Views.Installer.ApplicationsPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
        { typeof(Views.Installer.DisplayPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
        { typeof(Views.Installer.GraphicsPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
        { typeof(Views.Installer.SecurityPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
        { typeof(Views.Installer.InstallPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } }
    };
}
