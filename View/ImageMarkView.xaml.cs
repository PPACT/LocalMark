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
        PreviewKeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ImageMarkViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            if (vm.SaveCommand.CanExecute(null)) vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CloseWindowCommand.Execute(null);
            e.Handled = true;
        }
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
        if (DataContext is ImageMarkViewModel vm)
        {
            // 坐标转换：原图像素 → Canvas 坐标
            foreach (var obj in results)
            {
                var (cx, cy, cw, ch) = PixelToCanvas(obj.X, obj.Y, obj.Width, obj.Height);
                vm.AddBox(cx, cy, cw, ch);
                vm.SelectedLabel = vm.Labels.FirstOrDefault(l => l.Name == obj.Label);
            }
            vm.AiStatus = $"AI 检测到 {results.Count} 个目标";
        }
    }

    private (double x, double y, double w, double h) PixelToCanvas(double px, double py, double pw, double ph)
    {
        if (ImgDisplay.Source is not BitmapImage bi) return (px, py, pw, ph);
        var oW = bi.PixelWidth; var oH = bi.PixelHeight;
        if (oW == 0 || oH == 0) return (px, py, pw, ph);
        var dW = ImgDisplay.ActualWidth; var dH = ImgDisplay.ActualHeight;
        var scale = Math.Min(dW / oW, dH / oH);
        var offX = (dW - oW * scale) / 2;
        var offY = (dH - oH * scale) / 2;
        return (px * scale + offX, py * scale + offY, pw * scale, ph * scale);
    }

    // ===== 鼠标绘制 =====

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = true;
        _startPoint = e.GetPosition(MarkCanvas);
        Canvas.SetLeft(MarkRect, _startPoint.X);
        Canvas.SetTop(MarkRect, _startPoint.Y);
        MarkRect.Width = 0; MarkRect.Height = 0;
        MarkRect.Visibility = Visibility.Visible;
        MarkCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;
        var pos = e.GetPosition(MarkCanvas);
        Canvas.SetLeft(MarkRect, Math.Min(pos.X, _startPoint.X));
        Canvas.SetTop(MarkRect, Math.Min(pos.Y, _startPoint.Y));
        MarkRect.Width = Math.Abs(pos.X - _startPoint.X);
        MarkRect.Height = Math.Abs(pos.Y - _startPoint.Y);
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = false;
        MarkCanvas.ReleaseMouseCapture();
        if (MarkRect.Width < 5 || MarkRect.Height < 5) { MarkRect.Visibility = Visibility.Collapsed; return; }
        if (DataContext is ImageMarkViewModel vm)
            vm.AddBox(
                Canvas.GetLeft(MarkRect), Canvas.GetTop(MarkRect),
                MarkRect.Width, MarkRect.Height);
    }

    private void ClearBox_Click(object sender, RoutedEventArgs e)
    {
        MarkRect.Visibility = Visibility.Collapsed;
        MarkRect.Width = 0; MarkRect.Height = 0;
        if (DataContext is ImageMarkViewModel vm) vm.ClearAllBoxesCommand.Execute(null);
    }
}
