using Board.Domain;
using Board.UI.ViewModel;
using System.Windows;

namespace ConnectFour;

/// <summary>
/// Interaction logic for ConnectFourWindow.xaml
/// </summary>
public partial class ConnectFourWindow : Window
{
    public ConnectFourWindow(BoardViewModel boardViewModel)
    {
        InitializeComponent();
        DataContext = boardViewModel;
    }
}