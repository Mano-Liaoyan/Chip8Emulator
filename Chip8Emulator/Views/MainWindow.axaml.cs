using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Chip8Emulator.ViewModels;

namespace Chip8Emulator.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        DataContext = _viewModel = new MainWindowViewModel();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ShowHideControl(object? sender, RoutedEventArgs e)
    {
        _viewModel.ShowOpenGlControl = !_viewModel.ShowOpenGlControl;
    }
}