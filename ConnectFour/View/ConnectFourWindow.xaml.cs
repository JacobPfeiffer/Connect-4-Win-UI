using Board.UI.ViewModel;
using Microsoft.UI.Xaml;
using ReactiveUI;

namespace ConnectFour;

/// <summary>
/// Main window for Connect Four game. Implements IViewFor manually since ReactiveWindow doesn't exist in WinUI.
/// </summary>
public sealed partial class ConnectFourWindow : Window, IViewFor<BoardViewModel>
{
    public ConnectFourWindow(BoardViewModel boardViewModel)
    {
        this.InitializeComponent();

        // Set the window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(625, 500));

        ViewModel = boardViewModel;
    }

    private BoardViewModel? _viewModel;

    public BoardViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            if (Content is FrameworkElement root)
            {
                root.DataContext = value;
            }
        }
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (BoardViewModel?)value;
    }
}