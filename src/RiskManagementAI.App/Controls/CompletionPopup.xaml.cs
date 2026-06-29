using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RiskManagementAI.Core.Assist;

namespace RiskManagementAI.App.Controls;

public partial class CompletionPopup : UserControl
{
    public CompletionPopup()
    {
        InitializeComponent();
    }

    public event EventHandler<CompletionItem>? ItemAccepted;

    public bool IsCompletionOpen => RootPopup.IsOpen;

    public CompletionItem? SelectedCompletion => CompletionList.SelectedItem as CompletionItem;

    public void Show(TextBox placementTarget, IReadOnlyList<CompletionItem> items)
    {
        ArgumentNullException.ThrowIfNull(placementTarget);
        RootPopup.PlacementTarget = placementTarget;
        RootPopup.Placement = PlacementMode.Bottom;
        CompletionList.ItemsSource = items;
        CompletionList.SelectedIndex = items.Count > 0 ? 0 : -1;
        RootPopup.IsOpen = items.Count > 0;
        if (RootPopup.IsOpen)
        {
            CompletionList.Focus();
        }
    }

    public void Close()
    {
        RootPopup.IsOpen = false;
    }

    public bool TryAcceptSelected()
    {
        if (SelectedCompletion is null)
        {
            return false;
        }

        ItemAccepted?.Invoke(this, SelectedCompletion);
        return true;
    }

    private void OnCompletionListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = TryAcceptSelected();
    }

    private void OnCompletionListPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Tab)
        {
            e.Handled = TryAcceptSelected();
            return;
        }

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
