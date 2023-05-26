using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace JustifiedLayout;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        for (var j = 0; j < 40; ++j)
        {
            for (var i = 0; i < 24; ++i)
            {
                Images.Add(new(new(new(@$"ms-appx:///Assets/Images/{i}.jpg"))));
            }
        }

        InitializeComponent();
    }

    public List<CountImage> Images { get; set; } = new();

    private void UIElement_OnManipulationCompleted(object sender, object o)
    {
        var scrollViewer = (ScrollViewer)sender;
        TextBlock.Text = @$"VerticalOffset: {scrollViewer.VerticalOffset}
ExtentHeight: {scrollViewer.ExtentHeight}
ScrollableHeight: {scrollViewer.ScrollableHeight}
ViewportHeight: {scrollViewer.ViewportHeight}
ReachBottom: {scrollViewer.ScrollableHeight - ProgressRing.ActualHeight < scrollViewer.VerticalOffset}
VHCalc: {scrollViewer.ExtentHeight - scrollViewer.ScrollableHeight}";
    }
}

public class CountImage
{
    private int _num;

    public CountImage(BitmapImage image)
    {
        Image = image;
    }

    public int Num
    {
        get => _num++;
        set => _num = value;
    }

    public BitmapImage Image { get; set; }
}
