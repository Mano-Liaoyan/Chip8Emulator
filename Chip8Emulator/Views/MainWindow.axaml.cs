using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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


    private void ShowHideControl(object? sender, RoutedEventArgs e)
    {
        _viewModel.ShowOpenGlControl = !_viewModel.ShowOpenGlControl;
    }

    private async void LoadRom_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Chip-8 ROM",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Chip-8 ROM") { Patterns = ["*.ch8"] }
                ]
            });

            if (files.Count < 1) return;

            string filePath = files[0].Path.LocalPath;
            Chip8OpenGlControl.LoadRom(filePath);
            _viewModel.WindowTitle = $"Chip8Emulator - {files[0].Name}";
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);
        }
    }
}