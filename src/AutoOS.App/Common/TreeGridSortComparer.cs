using AutoOS.Views.Settings.BIOS;

namespace AutoOS.Common;

public class TreeGridSortComparer : IComparer<object>
{
    public string PropertyName { get; set; }

    public int Compare(object x, object y)
    {
        if (x is not BiosTreeNode node1 || y is not BiosTreeNode node2)
            return 0;

        if (node1.IsRoot && node2.IsRoot)
            return 0;

        if (node1.IsRoot) return -1;
        if (node2.IsRoot) return 1;

        var prop = typeof(BiosTreeNode).GetProperty(PropertyName);
        var val1 = prop?.GetValue(node1)?.ToString() ?? string.Empty;
        var val2 = prop?.GetValue(node2)?.ToString() ?? string.Empty;

        return string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
    }
}
