using System.Windows;
using System.Windows.Input;
using LocalMark.ViewModel;

namespace LocalMark.View;

public partial class TextMarkView : Window
{
    public TextMarkView()
    {
        InitializeComponent();
        PreviewKeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TextMarkViewModel vm)
        {
            if (vm.SaveCommand.CanExecute(null))
                vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && DataContext is TextMarkViewModel vm2)
        {
            vm2.CloseWindowCommand.Execute(null);
            e.Handled = true;
        }
    }
}
