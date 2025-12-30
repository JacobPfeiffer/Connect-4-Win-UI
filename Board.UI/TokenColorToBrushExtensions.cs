using Board.Domain;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace Board.UI;

public delegate SolidColorBrush TokenColorToBrushConverter(TokenState tokenState);

public static class TokenColorToBrushExtensions
{
    private static readonly SolidColorBrush WhiteBrush = new(Colors.White);
    private static readonly SolidColorBrush YellowBrush = new(Colors.Yellow);
    private static readonly SolidColorBrush RedBrush = new(Colors.Red);
    
    // Semi-transparent versions for preview tokens (70% opacity via alpha channel)
    private static readonly SolidColorBrush YellowPreviewBrush = new(Windows.UI.Color.FromArgb(153, 255, 255, 0));
    private static readonly SolidColorBrush RedPreviewBrush = new(Windows.UI.Color.FromArgb(153, 255, 0, 0));

    private static SolidColorBrush GetPlacedBrushForTokenState(this PlacedTokenState tokenState)
        => tokenState.Color switch
        {
            TokenColor.Yellow => YellowBrush,
            TokenColor.Red => RedBrush,
            _ => throw new NotImplementedException(),
        };

    private static SolidColorBrush GetPreviewBrushForTokenState(this PreviewTokenState tokenState)
        => tokenState.PreviewColor switch
        {
            TokenColor.Yellow => YellowPreviewBrush,
            TokenColor.Red => RedPreviewBrush,
            _ => throw new NotImplementedException(),
        };

    private static SolidColorBrush GetEmptyBrushForTokenState()
        => WhiteBrush;

    public static SolidColorBrush GetBrushForTokenState(this TokenState tokenState) =>
        tokenState.Match(
            onEmpty: _ => GetEmptyBrushForTokenState(),
            onPreview: previewToken => GetPreviewBrushForTokenState(previewToken),
            onPlaced: placedToken => GetPlacedBrushForTokenState(placedToken));
}