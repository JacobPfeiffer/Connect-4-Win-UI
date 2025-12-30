using Board.UI.ViewModel;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace Board.UI.View;

/// <summary>
/// Board column view for Connect Four game. Implements IViewFor manually since generic XAML roots aren't supported in WinUI.
/// </summary>
public sealed partial class BoardColumnView : UserControl, IViewFor<BoardColumnViewModel>
{
    public BoardColumnView()
    {
        this.InitializeComponent();
    }

    public BoardColumnViewModel? ViewModel
    {
        get => (BoardColumnViewModel?)DataContext;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (BoardColumnViewModel?)value;
    }
}
