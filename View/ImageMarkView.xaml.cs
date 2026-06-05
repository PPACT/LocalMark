using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LocalMark.Model;
using LocalMark.ViewModel;

namespace LocalMark.View;

public partial class ImageMarkView : Window
{
    private bool _isDrawing;
    private System.Windows.Point _startPoint;

    public ImageMarkView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ImageMarkViewModel oldVm)
            oldVm.AiMarkCompleted -= OnAiMarkCompleted;
        if (e.NewValue is ImageMarkViewModel newVm)
            newVm.AiMarkCompleted += OnAiMarkCompleted;
    }

    private void OnAiMarkCompleted(List<DetectedObject> results)
    {
        if (results.Count == 0) return;

        var first = results[0];
        if (DataContext is not ImageMarkViewModel vm) return;

        // 读取原图尺寸
        var imgSource = ImgDisplay.Source as BitmapImage;
        if (imgSource == null) return;
        var origW = imgSource.PixelWidth;
        var origH = imgSource.PixelHeight;
        if (origW == 0 || origH == 0) return;

        // Stretch="Uniform" 下的实际显示区域
        var displayW = ImgDisplay.ActualWidth;
        var displayH = ImgDisplay.ActualHeight;
        var scale = Math.Min(displayW / origW, displayH / origH);
        var offsetX = (displayW - origW * scale) / 2;
        var offsetY = (displayH - origH * scale) / 2;

        vm.BoxX = first.X * scale + offsetX;
        vm.BoxY = first.Y * scale + offsetY;
        vm.BoxWidth = first.Width * scale;
        vm.BoxHeight = first.Height * scale;

        // 更新 Canvas 上的矩形
        Canvas.SetLeft(MarkRect, vm.BoxX);
        Canvas.SetTop(MarkRect, vm.BoxY);
        MarkRect.Width = vm.BoxWidth;
        MarkRect.Height = vm.BoxHeight;
        MarkRect.Visibility = Visibility.Visible;

        // 自动匹配标签
        vm.SelectedLabel = vm.Labels.FirstOrDefault(l => l.Name == first.Label);
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
