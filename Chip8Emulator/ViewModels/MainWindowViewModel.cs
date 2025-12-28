using System;
using System.ComponentModel;

namespace Chip8Emulator.ViewModels;

public class MainWindowViewModel : EasyNotifyPropertyChanged
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

    public string WindowTitle
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = "Chip8Emulator";

    private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShowOpenGlControl):
                Console.WriteLine($"{e.PropertyName} changed by {sender} to {ShowOpenGlControl}.");
                break;
            case nameof(WindowTitle):
                Console.WriteLine($"{e.PropertyName} changed by {sender} to {WindowTitle}.");
                break;
        }
    }
}