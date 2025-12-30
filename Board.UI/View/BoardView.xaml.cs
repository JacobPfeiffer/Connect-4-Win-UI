using Board.UI.ViewModel;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace Board.UI.View;

/// <summary>
/// Board view for Connect Four game. Implements IViewFor manually since generic XAML roots aren't supported in WinUI.
/// </summary>
public sealed partial class BoardView : UserControl, IViewFor<BoardViewModel>
{
    public BoardView()
    {
        this.InitializeComponent();
    }

    public BoardViewModel? ViewModel
    {
        get => (BoardViewModel?)DataContext;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (BoardViewModel?)value;
    }
}