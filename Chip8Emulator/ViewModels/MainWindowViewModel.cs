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

    public bool ShiftQuirksEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = false;

    public double CpuFrequency
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = 700.0;

    public string ProgramCounter
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = "0000";

    public string IndexRegister
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = "0000";

    public string Opcode
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = "0000";

    public string StackPointer
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = "00";

    public string DelayTimer
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = "00";

    public string SoundTimer
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = "00";

    public byte DelayTimerValue
    {
        get;
        set { field = value; OnPropertyChanged(); }
    }

    public byte SoundTimerValue
    {
        get;
        set { field = value; OnPropertyChanged(); }
    }

    public void UpdateCpuState(CPU cpu)
    {
        ProgramCounter = cpu.PC.ToString("X4");
        IndexRegister = cpu.I.ToString("X4");
        Opcode = cpu.Opcode.ToString("X4");
        StackPointer = cpu.SP.ToString("X2");
        DelayTimer = cpu.DelayTimer.ToString("X2");
        SoundTimer = cpu.SoundTimer.ToString("X2");
        DelayTimerValue = cpu.DelayTimer;
        SoundTimerValue = cpu.SoundTimer;

        for (int i = 0; i < 16; i++)
        {
            string newVal = cpu.Registers[i].ToString("X2");
            if (Registers[i].Value != newVal) Registers[i].Value = newVal;
        }

        int[] displayOrder = [0x1, 0x2, 0x3, 0xC, 0x4, 0x5, 0x6, 0xD, 0x7, 0x8, 0x9, 0xE, 0xA, 0x0, 0xB, 0xF];
        for (int i = 0; i < 16; i++)
        {
            bool isPressed = cpu.Keypad[displayOrder[i]] != 0;
            if (Keypad[i].IsActive != isPressed) Keypad[i].IsActive = isPressed;
        }

        int sp = cpu.SP;
        while (Stack.Count > sp) Stack.RemoveAt(Stack.Count - 1);
        while (Stack.Count < sp) Stack.Add("0000");
        for (int i = 0; i < sp; i++)
        {
            string newVal = cpu.Stack[i].ToString("X4");
            if (Stack[i] != newVal) Stack[i] = newVal;
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