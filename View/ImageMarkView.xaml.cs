using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LocalMark.ViewModel;

namespace LocalMark.View;

public partial class ImageMarkView : Window
{
    private bool _isDrawing;
    private System.Windows.Point _startPoint;

    public ImageMarkView()
    {
        InitializeComponent();
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = true;
        _startPoint = e.GetPosition(MarkCanvas);

        Canvas.SetLeft(MarkRect, _startPoint.X);
        Canvas.SetTop(MarkRect, _startPoint.Y);
        MarkRect.Width = 0;
        MarkRect.Height = 0;
        MarkRect.Visibility = Visibility.Visible;

        MarkCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;

        var pos = e.GetPosition(MarkCanvas);
        var x = System.Math.Min(pos.X, _startPoint.X);
        var y = System.Math.Min(pos.Y, _startPoint.Y);
        var w = System.Math.Abs(pos.X - _startPoint.X);
        var h = System.Math.Abs(pos.Y - _startPoint.Y);

        Canvas.SetLeft(MarkRect, x);
        Canvas.SetTop(MarkRect, y);
        MarkRect.Width = w;
        MarkRect.Height = h;
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = false;
        MarkCanvas.ReleaseMouseCapture();

        // 同步到 ViewModel
        if (DataContext is ImageMarkViewModel vm)
        {
            vm.BoxX = Canvas.GetLeft(MarkRect);
            vm.BoxY = Canvas.GetTop(MarkRect);
            vm.BoxWidth = MarkRect.Width;
            vm.BoxHeight = MarkRect.Height;
        }
    }

    private void ClearBox_Click(object sender, RoutedEventArgs e)
    {
        MarkRect.Visibility = Visibility.Collapsed;
        MarkRect.Width = 0;
        MarkRect.Height = 0;

        if (DataContext is ImageMarkViewModel vm)
        {
            vm.BoxX = 0;
            vm.BoxY = 0;
            vm.BoxWidth = 0;
            vm.BoxHeight = 0;
        }
    }
}
