using System.Runtime.CompilerServices;

namespace AutoOS.Common;

public static class CommandBarExtensions
{
    private static readonly ConditionalWeakTable<CommandBar, Dictionary<AppBarElementContainer, Thickness>> _marginsMap = [];

    public static readonly DependencyProperty ApplyOverflowIndentProperty =
        DependencyProperty.RegisterAttached(
            "ApplyOverflowIndent",
            typeof(bool),
            typeof(CommandBarExtensions),
            new PropertyMetadata(false, OnApplyOverflowIndentChanged));

    public static bool GetApplyOverflowIndent(DependencyObject obj)
    {
        return (bool)obj.GetValue(ApplyOverflowIndentProperty);
    }

    public static void SetApplyOverflowIndent(DependencyObject obj, bool value)
    {
        obj.SetValue(ApplyOverflowIndentProperty, value);
    }

    public static readonly DependencyProperty CommandAlignmentProperty =
        DependencyProperty.RegisterAttached(
            "CommandAlignment",
            typeof(HorizontalAlignment),
            typeof(CommandBarExtensions),
            new PropertyMetadata(HorizontalAlignment.Left, OnCommandAlignmentChanged));

    public static HorizontalAlignment GetCommandAlignment(DependencyObject obj)
    {
        return (HorizontalAlignment)obj.GetValue(CommandAlignmentProperty);
    }

    public static void SetCommandAlignment(DependencyObject obj, HorizontalAlignment value)
    {
        obj.SetValue(CommandAlignmentProperty, value);
    }

    public static readonly DependencyProperty OverflowButtonAlignmentProperty =
        DependencyProperty.RegisterAttached(
            "OverflowButtonAlignment",
            typeof(HorizontalAlignment),
            typeof(CommandBarExtensions),
            new PropertyMetadata(HorizontalAlignment.Right, OnOverflowButtonAlignmentChanged));

    public static HorizontalAlignment GetOverflowButtonAlignment(DependencyObject obj)
    {
        return (HorizontalAlignment)obj.GetValue(OverflowButtonAlignmentProperty);
    }

    public static void SetOverflowButtonAlignment(DependencyObject obj, HorizontalAlignment value)
    {
        obj.SetValue(OverflowButtonAlignmentProperty, value);
    }

    private static void OnApplyOverflowIndentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CommandBar commandBar)
            return;

        bool newValue = (bool)e.NewValue;

        if (newValue)
        {
            commandBar.Opening += CommandBar_Opening;
            commandBar.Closed += CommandBar_Closed;
            
            if (commandBar.IsOpen)
            {
                ApplyOverflowContainerMargins(commandBar);
            }

            if (commandBar.IsLoaded)
            {
                UpdateCommandAlignment(commandBar, GetCommandAlignment(commandBar));
                UpdateOverflowButtonAlignment(commandBar, GetOverflowButtonAlignment(commandBar));
            }
            else
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (sender, args) =>
                {
                    commandBar.Loaded -= loadedHandler;
                    UpdateCommandAlignment(commandBar, GetCommandAlignment(commandBar));
                    UpdateOverflowButtonAlignment(commandBar, GetOverflowButtonAlignment(commandBar));
                };
                commandBar.Loaded += loadedHandler;
            }
        }
        else
        {
            commandBar.Opening -= CommandBar_Opening;
            commandBar.Closed -= CommandBar_Closed;
            RestoreOverflowContainerMargins(commandBar);
        }
    }

    private static void OnCommandAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CommandBar commandBar)
            return;

        if (commandBar.IsLoaded)
        {
            UpdateCommandAlignment(commandBar, (HorizontalAlignment)e.NewValue);
        }
        else
        {
            RoutedEventHandler loadedHandler = null;
            loadedHandler = (sender, args) =>
            {
                commandBar.Loaded -= loadedHandler;
                UpdateCommandAlignment(commandBar, GetCommandAlignment(commandBar));
            };
            commandBar.Loaded += loadedHandler;
        }
    }

    private static void OnOverflowButtonAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CommandBar commandBar)
            return;

        if (commandBar.IsLoaded)
        {
            UpdateOverflowButtonAlignment(commandBar, (HorizontalAlignment)e.NewValue);
        }
        else
        {
            RoutedEventHandler loadedHandler = null;
            loadedHandler = (sender, args) =>
            {
                commandBar.Loaded -= loadedHandler;
                UpdateOverflowButtonAlignment(commandBar, GetOverflowButtonAlignment(commandBar));
            };
            commandBar.Loaded += loadedHandler;
        }
    }

    private static void UpdateCommandAlignment(CommandBar commandBar, HorizontalAlignment alignment)
    {
        try
        {
            commandBar.ApplyTemplate();
			var primaryItemsControl = FindVisualChild<ItemsControl>(commandBar, "PrimaryItemsControl");
			primaryItemsControl?.HorizontalAlignment = alignment;
        }
        catch { }
    }

    private static void UpdateOverflowButtonAlignment(CommandBar commandBar, HorizontalAlignment alignment)
    {
        try
        {
            commandBar.ApplyTemplate();
			var moreButton = FindVisualChild<Button>(commandBar, "MoreButton");
			moreButton?.HorizontalAlignment = alignment;
        }
        catch { }
    }

    private static T FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
    {
        for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(obj, i);
            if (child is T t && (child as FrameworkElement)?.Name == name)
            {
                return t;
            }
            var childOfChild = FindVisualChild<T>(child, name);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }

    private static void CommandBar_Opening(object sender, object e)
    {
        if (sender is CommandBar commandBar)
        {
            ApplyOverflowContainerMargins(commandBar);
        }
    }

    private static void CommandBar_Closed(object sender, object e)
    {
        if (sender is CommandBar commandBar)
        {
            RestoreOverflowContainerMargins(commandBar);
        }
    }

    private static void ApplyOverflowContainerMargins(CommandBar commandBar)
    {
        var margins = _marginsMap.GetOrCreateValue(commandBar);

        foreach (var container in commandBar.PrimaryCommands.OfType<AppBarElementContainer>())
        {
            if (container.IsInOverflow)
            {
                margins.TryAdd(container, container.Margin);
                container.Margin = new Thickness(32, 0, 0, 0);
            }
        }

        foreach (var container in commandBar.SecondaryCommands.OfType<AppBarElementContainer>())
        {
            margins.TryAdd(container, container.Margin);
            container.Margin = new Thickness(32, 0, 0, 0);
        }
    }

    private static void RestoreOverflowContainerMargins(CommandBar commandBar)
    {
        if (_marginsMap.TryGetValue(commandBar, out var margins))
        {
            foreach (var (container, margin) in margins)
            {
                container.Margin = margin;
            }
            margins.Clear();
        }
    }
}
