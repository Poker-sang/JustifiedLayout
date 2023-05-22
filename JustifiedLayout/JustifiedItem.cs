using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace JustifiedLayout;

internal class JustifiedItem
{
    public JustifiedItem(int index) => Index = index;

    public int Index { get; }

    public Size? DesiredSize { get; internal set; }

    public Size? Measure { get; internal set; }

    public Point? Position { get; internal set; }

    public UIElement? Element { get; internal set; }
}
