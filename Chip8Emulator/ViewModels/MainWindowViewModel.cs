using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Chip8Emulator.Core;

namespace Chip8Emulator.ViewModels;

public class MainWindowViewModel : EasyNotifyPropertyChanged
{
    public MainWindowViewModel()
    {
        for (int i = 0; i < 16; i++) Registers.Add(new RegisterItem { Name = $"V{i:X}", Value = "00" });
        // Keypad items: Name = Key char (1, 2, 3, C..), Value = "0" or "1" (will use for color binding)
        // Mapping:
        // 1 2 3 C
        // 4 5 6 D
        // 7 8 9 E
        // A 0 B F
        // However, the keypad index in CPU is 0-F.
        // We want to display them in the 4x4 grid layout.
        // The indices 0-F map to:
        // 1->1, 2->2, 3->3, C->4
        // 4->Q, 5->W, 6->E, D->R
        // 7->A, 8->S, 9->D, E->F
        // A->Z, 0->X, B->C, F->V
        // So let's just initialize 16 items and bind them to the CPU indices.
        // We will order them in the UI grid order for easier display:
        // Row 0: 1, 2, 3, C (Indices: 1, 2, 3, C)
        // Row 1: 4, 5, 6, D (Indices: 4, 5, 6, D)
        // Row 2: 7, 8, 9, E (Indices: 7, 8, 9, E)
        // Row 3: A, 0, B, F (Indices: A, 0, B, F)

        // Let's create a list of indices in display order
        string[] labels = ["1", "2", "3", "C", "4", "5", "6", "D", "7", "8", "9", "E", "A", "0", "B", "F"];

        for (int i = 0; i < 16; i++) Keypad.Add(new RegisterItem { Name = labels[i], Value = "False" });

        PropertyChanged += PropertyChangedHandler;
    }

    public ObservableCollection<RegisterItem> Registers { get; } = [];
    public ObservableCollection<RegisterItem> Keypad { get; } = [];
    public ObservableCollection<string> Stack { get; } = [];

    public bool ShowOpenGlControl
    {
        get;
        set
        {
            field = value; // Assuming 'field' is a backing field managed by EasyNotifyPropertyChanged or a typo for a private field
            OnPropertyChanged();
        }
    } = true;

    public string WindowTitle
    {
        get;
        set
        {
            field = value; // Assuming 'field' is a backing field managed by EasyNotifyPropertyChanged or a typo for a private field
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
        // ... existing updates ...
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

        // Update Keypad
        int[] displayOrder = [0x1, 0x2, 0x3, 0xC, 0x4, 0x5, 0x6, 0xD, 0x7, 0x8, 0x9, 0xE, 0xA, 0x0, 0xB, 0xF];
        for (int i = 0; i < 16; i++)
        {
            int cpuKeyIndex = displayOrder[i];
            bool isPressed = cpu.Keypad[cpuKeyIndex] != 0;
            if (Keypad[i].IsActive != isPressed) Keypad[i].IsActive = isPressed;
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
            if (Stack[i] == newVal) continue;

            Stack[i] = newVal;
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

        public bool IsActive
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
        }
    }
}