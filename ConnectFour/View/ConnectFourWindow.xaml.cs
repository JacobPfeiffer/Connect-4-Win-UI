using Board.Domain;
using Board.UI.ViewModel;
using Microsoft.UI.Xaml;

namespace ConnectFour;

/// <summary>
/// Main window for Connect Four game
/// </summary>
public sealed partial class ConnectFourWindow : Window
{
    public ConnectFourWindow(BoardViewModel boardViewModel)
    {
        this.InitializeComponent();
        
        // Set the window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(600, 650));
        
        // Set DataContext on the root content
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = boardViewModel;
        }
    }
}