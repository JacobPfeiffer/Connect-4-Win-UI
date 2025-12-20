using Microsoft.UI.Xaml.Controls;

namespace Board.UI.View;

/// <summary>
/// Board view for Connect Four game
/// </summary>
public sealed partial class BoardView : UserControl
{
    public BoardView()
    {
        this.InitializeComponent();
        // DataContext is set by parent window or DI
    }
}