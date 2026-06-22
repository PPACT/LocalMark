using System.Windows;

namespace LocalMark.View;

public enum DuplicateAction { Overwrite, Skip, Cancel }

public partial class DuplicateDialog : Window
{
    public DuplicateAction Result { get; private set; } = DuplicateAction.Cancel;

    public DuplicateDialog(IEnumerable<string> duplicates)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        FileList.ItemsSource = duplicates.ToList();
    }

    private void OverwriteBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = DuplicateAction.Overwrite;
        Close();
    }

    private void SkipBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = DuplicateAction.Skip;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
