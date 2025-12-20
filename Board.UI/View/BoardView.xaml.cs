using Board.Domain;
using Board.UI.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Board.UI.View;

/// <summary>
/// Interaction logic for BoardView.xaml
/// </summary>
public partial class BoardView : UserControl
{
    public BoardView()
    {
        InitializeComponent();
        // DataContext is set by parent window or DI
    }
}