using System;
using System.ComponentModel;

namespace Chip8Emulator.ViewModels;

public partial class MainWindowViewModel : EasyNotifyPropertyChanged
{
    public MainWindowViewModel()
    {
        PropertyChanged += PropertyChangedHandler;
    }

    public bool ShowOpenGlControl
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        Console.WriteLine($"{e.PropertyName} changed by {sender} to {ShowOpenGlControl}.");
    }
}