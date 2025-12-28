using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Chip8Emulator.Core;

namespace Chip8Emulator.ViewModels;

public class MainWindowViewModel : EasyNotifyPropertyChanged
{
    public class RegisterItem : EasyNotifyPropertyChanged
    {
        public string Name { get; set; } = "";

        public string Value
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                }
            }
        } = "00";
    }

    public MainWindowViewModel()
    {
        for (int i = 0; i < 16; i++) Registers.Add(new RegisterItem { Name = $"V{i:X}", Value = "00" });
        PropertyChanged += PropertyChangedHandler;
    }

    public ObservableCollection<RegisterItem> Registers { get; } = new();
    public ObservableCollection<string> Stack { get; } = new();

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

    public string ProgramCounter { get; set; } = "0000";

    public string IndexRegister { get; set; } = "0000";

    public string Opcode { get; set; } = "0000";

    public string StackPointer { get; set; } = "00";

    public string DelayTimer { get; set; } = "00";

    public string SoundTimer { get; set; } = "00";

    public void UpdateCpuState(CPU cpu)
    {
        if (ProgramCounter != cpu.PC.ToString("X4"))
        {
            ProgramCounter = cpu.PC.ToString("X4");
            OnPropertyChanged(nameof(ProgramCounter));
        }

        if (IndexRegister != cpu.I.ToString("X4"))
        {
            IndexRegister = cpu.I.ToString("X4");
            OnPropertyChanged(nameof(IndexRegister));
        }

        if (Opcode != cpu.Opcode.ToString("X4"))
        {
            Opcode = cpu.Opcode.ToString("X4");
            OnPropertyChanged(nameof(Opcode));
        }

        if (StackPointer != cpu.SP.ToString("X2"))
        {
            StackPointer = cpu.SP.ToString("X2");
            OnPropertyChanged(nameof(StackPointer));
        }

        if (DelayTimer != cpu.DelayTimer.ToString("X2"))
        {
            DelayTimer = cpu.DelayTimer.ToString("X2");
            OnPropertyChanged(nameof(DelayTimer));
        }

        if (SoundTimer != cpu.SoundTimer.ToString("X2"))
        {
            SoundTimer = cpu.SoundTimer.ToString("X2");
            OnPropertyChanged(nameof(SoundTimer));
        }

        for (int i = 0; i < 16; i++)
        {
            string newVal = cpu.Registers[i].ToString("X2");

            if (Registers[i].Value != newVal) Registers[i].Value = newVal;
        }

        // Update Stack
        // The stack grows from 0 to SP. Valid items are 0 to SP-1.
        int sp = cpu.SP;

        // Remove items if current stack is larger than SP
        while (Stack.Count > sp) Stack.RemoveAt(Stack.Count - 1);

        // Add items if current stack is smaller than SP
        while (Stack.Count < sp)
            // Add placeholder, will be updated below
            Stack.Add("0000");

        // Update values
        for (int i = 0; i < sp; i++)
        {
            string newVal = cpu.Stack[i].ToString("X4");
            if (Stack[i] != newVal) Stack[i] = newVal;
        }
    }

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