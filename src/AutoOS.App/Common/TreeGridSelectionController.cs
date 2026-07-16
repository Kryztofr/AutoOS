using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.System;

namespace AutoOS.Common;

public sealed partial class TreeGridSelectionController : TreeGridRowSelectionController
{
	private readonly SfTreeGrid _treeGrid;

	public TreeGridSelectionController(SfTreeGrid treeGrid) : base(treeGrid)
	{
		_treeGrid = treeGrid;
		treeGrid.LostFocus += (_, _) =>
		{
			if (CurrentCellManager.CurrentCell?.IsEditing == true)
				return;

			var focused = FocusManager.GetFocusedElement(_treeGrid.XamlRoot) as DependencyObject;
			if (focused == null)
			{
				ClearSelections(false);
				return;
			}

			var current = focused;
			while (current != null)
			{
				if (current == _treeGrid)
					return;
				current = VisualTreeHelper.GetParent(current);
			}

			ClearSelections(false);
		};
	}

	protected override void ProcessKeyDown(KeyRoutedEventArgs args)
	{
		if (args.Key == VirtualKey.Enter && CurrentCellManager.CurrentCell?.IsEditing != true)
		{
			CurrentCellManager.BeginEdit();
			args.Handled = true;
			return;
		}

		base.ProcessKeyDown(args);
	}
}
