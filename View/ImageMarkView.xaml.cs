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

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter
            && DataContext is ImageMarkViewModel vm)
        {
            if (vm.SaveCommand.CanExecute(null))
                vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape
                 && DataContext is ImageMarkViewModel vm2)
        {
            vm2.CloseWindowCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Left && _aiResults is { Count: > 1 })
        {
            PrevDetection_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Right && _aiResults is { Count: > 1 })
        {
            NextDetection_Click(sender, e);
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

    private List<DetectedObject>? _aiResults;
    private int _currentIndex;

    private void OnAiMarkCompleted(List<DetectedObject> results)
    {
        if (results.Count == 0) return;

        _aiResults = results;
        _currentIndex = 0;
        ApplyDetection(_currentIndex);
        UpdateNavButtons();
    }

    private void PrevDetection_Click(object sender, RoutedEventArgs e)
    {
        if (_aiResults == null || _currentIndex <= 0) return;
        _currentIndex--;
        ApplyDetection(_currentIndex);
        UpdateNavButtons();
    }

    private void NextDetection_Click(object sender, RoutedEventArgs e)
    {
        if (_aiResults == null || _currentIndex >= _aiResults.Count - 1) return;
        _currentIndex++;
        ApplyDetection(_currentIndex);
        UpdateNavButtons();
    }

    private void UpdateNavButtons()
    {
        var count = _aiResults?.Count ?? 0;
        PrevBtn.IsEnabled = count > 1 && _currentIndex > 0;
        NextBtn.IsEnabled = count > 1 && _currentIndex < count - 1;
        NavLabel.Text = count > 1 ? $"{_currentIndex + 1}/{count}" : "";
        NavPanel.Visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyDetection(int index)
    {
        if (_aiResults == null || index >= _aiResults.Count) return;
        if (DataContext is not ImageMarkViewModel vm) return;

        var obj = _aiResults[index];

        var imgSource = ImgDisplay.Source as BitmapImage;
        if (imgSource == null) return;
        var origW = imgSource.PixelWidth;
        var origH = imgSource.PixelHeight;
        if (origW == 0 || origH == 0) return;

        var displayW = ImgDisplay.ActualWidth;
        var displayH = ImgDisplay.ActualHeight;
        var scale = Math.Min(displayW / origW, displayH / origH);
        var offsetX = (displayW - origW * scale) / 2;
        var offsetY = (displayH - origH * scale) / 2;

        vm.BoxX = obj.X * scale + offsetX;
        vm.BoxY = obj.Y * scale + offsetY;
        vm.BoxWidth = obj.Width * scale;
        vm.BoxHeight = obj.Height * scale;

        Canvas.SetLeft(MarkRect, vm.BoxX);
        Canvas.SetTop(MarkRect, vm.BoxY);
        MarkRect.Width = vm.BoxWidth;
        MarkRect.Height = vm.BoxHeight;
        MarkRect.Visibility = Visibility.Visible;

        vm.SelectedLabel = vm.Labels.FirstOrDefault(l => l.Name == obj.Label);
        vm.AiStatus = _aiResults.Count > 1
            ? $"AI 检测到 {_aiResults.Count} 个目标，当前第 {index + 1} 个"
            : $"AI 检测到 {_aiResults.Count} 个目标";
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

        _aiResults = null;
        _currentIndex = 0;
        NavPanel.Visibility = Visibility.Collapsed;

        if (DataContext is ImageMarkViewModel vm)
        {
            vm.BoxX = 0;
            vm.BoxY = 0;
            vm.BoxWidth = 0;
            vm.BoxHeight = 0;
        }
    }
}
