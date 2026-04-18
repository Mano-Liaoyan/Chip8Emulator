# Chip-8 Emulator Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all identified bugs, remove dead code, improve code quality, and add speed control, pause/reset, color palette, and aspect-ratio preservation features.

**Architecture:** Bugs and cleanup are isolated one-file changes. Features are layered: CPU gets new public API, `Chip8InterfaceOpenGlControl` owns runtime state (speed, pause, colors), and `MainWindowViewModel` exposes everything to the XAML UI via bindings.

**Tech Stack:** .NET 10, C# 14, Avalonia 11, OpenTK 4, OpenAL (via OpenTK.Audio), xUnit (new test project)

---

## File Map

| File | Change |
|------|--------|
| `Chip8Emulator/Chip8InterfaceOpenGlControl.cs` | Fix CpuPeriod; add speed, pause, color, aspect ratio |
| `Chip8Emulator/Core/CPU.cs` | Add `LoadROM(ReadOnlySpan<byte>)`; stack bounds; Fx0A release; shift quirk |
| `Chip8Emulator/Core/SoundPlayer.cs` | Fix `ALFormat.Stereo16` → `ALFormat.Mono16` |
| `Chip8Emulator/CubeRenderingTkOpenGlControl.cs` | **Delete** |
| `Chip8Emulator/ViewModels/MainWindowViewModel.cs` | Consistent properties; remove debug handler; add speed/pause/color bindings |
| `Chip8Emulator/Views/MainWindow.axaml` | Timer bars; speed slider; pause/reset buttons; color selectors |
| `Chip8Emulator/Views/MainWindow.axaml.cs` | Pause/reset click handlers; color change handler |
| `Chip8Emulator.Tests/Chip8Emulator.Tests.csproj` | **New** xUnit test project |
| `Chip8Emulator.Tests/CpuTests.cs` | **New** CPU unit tests |

---

## Task 1: Fix CPU Clock Speed Bug

**Files:**
- Modify: `Chip8Emulator/Chip8InterfaceOpenGlControl.cs:14`

- [ ] **Step 1: Fix the constant**

In `Chip8InterfaceOpenGlControl.cs`, change line 14:
```csharp
// Before
private const double CpuPeriod = 2000.0 / CpuFrequency;

// After
private const double CpuPeriod = 1000.0 / CpuFrequency;
```

- [ ] **Step 2: Build and verify**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Chip8Emulator/Chip8InterfaceOpenGlControl.cs
git commit -m "fix: correct CPU clock speed from 350Hz to 700Hz"
```

---

## Task 2: Fix SoundPlayer Audio Format Bug

**Files:**
- Modify: `Chip8Emulator/Core/SoundPlayer.cs:42`

- [ ] **Step 1: Fix the format and regenerate buffer as mono**

In `SoundPlayer.cs`, replace lines 26–46 (the buffer generation and `AL.BufferData` call):
```csharp
// Replace the buffer generation block:
const int sampleRate = 44100;
const double frequency = 440.0; // A4
const int dataCount = sampleRate; // 1 second of mono samples
short[] bufferData = new short[dataCount];

for (int i = 0; i < dataCount; i++)
{
    double angle = Math.PI * 2.0 * frequency * i / sampleRate;
    bufferData[i] = (short)(Math.Sin(angle) * (short.MaxValue * 0.5));
}

// 4. Create OpenAL Buffer
_buffer = AL.GenBuffer();
AL.BufferData(_buffer, ALFormat.Mono16, bufferData, sampleRate);
```

- [ ] **Step 2: Build and verify**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Chip8Emulator/Core/SoundPlayer.cs
git commit -m "fix: use Mono16 format for sound buffer, fixes 220Hz pitch bug"
```

---

## Task 3: Create CPU Test Project

**Files:**
- Create: `Chip8Emulator.Tests/Chip8Emulator.Tests.csproj`
- Create: `Chip8Emulator.Tests/CpuTests.cs`
- Modify: `Chip8Emulator/Core/CPU.cs` — add `LoadROM(ReadOnlySpan<byte>)` overload

- [ ] **Step 1: Add `LoadROM(ReadOnlySpan<byte>)` overload to CPU.cs**

In `CPU.cs`, after the existing `LoadROM(string romPath)` method, add:
```csharp
public void LoadROM(ReadOnlySpan<byte> rom)
{
    Reset();
    for (int i = 0; i < rom.Length && startAddress + i < memory.Length; i++)
        memory[startAddress + i] = rom[i];
}
```

Also refactor the existing `LoadROM(string)` to use it:
```csharp
public void LoadROM(string romPath)
{
    if (!File.Exists(romPath))
        throw new FileNotFoundException($"ROM file not found: {romPath}");
    LoadROM(File.ReadAllBytes(romPath));
}
```

- [ ] **Step 2: Create the test project file**

Create `Chip8Emulator.Tests/Chip8Emulator.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Chip8Emulator\Chip8Emulator.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add test project to solution**

```bash
cd C:/Users/yanev/source/repos/Chip8Emulator
dotnet sln add Chip8Emulator.Tests/Chip8Emulator.Tests.csproj
```
Expected: `Project added to the solution.`

- [ ] **Step 4: Create initial test file with a smoke test**

Create `Chip8Emulator.Tests/CpuTests.cs`:
```csharp
using Chip8Emulator.Core;
using Xunit;

namespace Chip8Emulator.Tests;

public class CpuTests
{
    private static CPU CreateCpu(params byte[] rom)
    {
        var cpu = new CPU();
        cpu.LoadROM(rom);
        return cpu;
    }

    [Fact]
    public void LoadROM_SetsPC_To0x200()
    {
        var cpu = CreateCpu(0x00, 0xE0);
        Assert.Equal(0x200, cpu.PC);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
cd C:/Users/yanev/source/repos/Chip8Emulator
dotnet test Chip8Emulator.Tests
```
Expected: `Passed! - Failed: 0, Passed: 1`

- [ ] **Step 6: Commit**

```bash
git add Chip8Emulator/Core/CPU.cs Chip8Emulator.Tests/ Chip8Emulator.sln
git commit -m "test: add Chip8.Tests project with CPU unit test infrastructure"
```

---

## Task 4: Fix Stack Bounds Checks

**Files:**
- Modify: `Chip8Emulator/Core/CPU.cs` — `OP_2nnn`, `OP_00EE`
- Modify: `Chip8Emulator.Tests/CpuTests.cs` — add stack tests

- [ ] **Step 1: Write failing tests**

Add to `Chip8Emulator.Tests/CpuTests.cs`:
```csharp
[Fact]
public void OP_2nnn_AtMaxStack_ThrowsInvalidOperationException()
{
    var cpu = new CPU();
    // Fill the stack by calling 16 nested subroutines
    // Each CALL is 0x2NNN. We'll call address 0x202 from 0x200 repeatedly.
    // ROM: 0x2202 (call 0x202) repeated 16 times, then a NOP (0x00E0)
    var rom = new List<byte>();
    for (int i = 0; i < 16; i++) { rom.Add(0x22); rom.Add(0x00); } // 0x2200 = CALL 0x200
    cpu.LoadROM(rom.ToArray());
    Assert.Throws<InvalidOperationException>(() =>
    {
        for (int i = 0; i < 17; i++) cpu.Cycle();
    });
}

[Fact]
public void OP_00EE_AtEmptyStack_ThrowsInvalidOperationException()
{
    // RET with SP=0 should throw
    var cpu = CreateCpu(0x00, 0xEE); // RET
    Assert.Throws<InvalidOperationException>(() => cpu.Cycle());
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test Chip8Emulator.Tests --filter "OP_2nnn_AtMaxStack|OP_00EE_AtEmptyStack"
```
Expected: Both fail (no exception currently thrown).

- [ ] **Step 3: Add bounds checks to CPU.cs**

In `CPU.cs`, update `OP_2nnn`:
```csharp
private void OP_2nnn()
{
    if (SP >= Stack.Length)
        throw new InvalidOperationException("Stack overflow: CALL with full stack.");
    ushort address = (ushort)(Opcode & 0x0FFF);
    Stack[SP] = PC;
    SP += 1;
    PC = address;
}
```

Update `OP_00EE`:
```csharp
private void OP_00EE()
{
    if (SP == 0)
        throw new InvalidOperationException("Stack underflow: RET with empty stack.");
    SP -= 1;
    PC = Stack[SP];
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test Chip8Emulator.Tests --filter "OP_2nnn_AtMaxStack|OP_00EE_AtEmptyStack"
```
Expected: Both pass.

- [ ] **Step 5: Commit**

```bash
git add Chip8Emulator/Core/CPU.cs Chip8Emulator.Tests/CpuTests.cs
git commit -m "fix: add stack overflow and underflow guards to CALL/RET opcodes"
```

---

## Task 5: Fix Fx0A Wait-for-Key-Release

**Files:**
- Modify: `Chip8Emulator/Core/CPU.cs` — `OP_Fx0A`, add `_waitingForKeyRelease` field
- Modify: `Chip8Emulator.Tests/CpuTests.cs` — add Fx0A tests

- [ ] **Step 1: Write failing test**

Add to `CpuTests.cs`:
```csharp
[Fact]
public void OP_Fx0A_DoesNotAdvance_UntilKeyReleased()
{
    // 0xF00A = wait for key, store in V0
    var cpu = CreateCpu(0xF0, 0x0A);

    // Simulate key 3 pressed
    cpu.SetKey(3, true);
    cpu.Cycle(); // should NOT advance past 0x202 yet (key still held)
    Assert.Equal(0x200, cpu.PC); // retried (PC decremented back)

    // Now release key 3
    cpu.SetKey(3, false);
    cpu.Cycle(); // now it should store and advance
    Assert.Equal(0x202, cpu.PC);
    Assert.Equal(3, cpu.Registers[0]);
}
```

- [ ] **Step 2: Run to confirm it fails**

```bash
dotnet test Chip8Emulator.Tests --filter "OP_Fx0A_DoesNotAdvance"
```
Expected: Fail.

- [ ] **Step 3: Add key-release tracking to CPU**

In `CPU.cs`, add a private field at the top of the class (near `_random`):
```csharp
private int _waitingForKeyRelease = -1; // -1 = not waiting; 0-15 = key index waiting for release
```

Replace the `OP_Fx0A` method:
```csharp
private void OP_Fx0A()
{
    byte x = (byte)((Opcode & 0x0F00) >> 8);

    // Phase 1: waiting for a key to be pressed
    if (_waitingForKeyRelease == -1)
    {
        for (byte i = 0; i < 16; i++)
        {
            if (Keypad[i] != 0)
            {
                _waitingForKeyRelease = i;
                Registers[x] = i;
                break;
            }
        }
        // No key pressed yet — re-execute this instruction next cycle
        if (_waitingForKeyRelease == -1) PC -= 2;
        else PC -= 2; // Also stay here until release
        return;
    }

    // Phase 2: waiting for the pressed key to be released
    if (Keypad[_waitingForKeyRelease] != 0)
    {
        // Key still held — re-execute
        PC -= 2;
        return;
    }

    // Key released — done
    _waitingForKeyRelease = -1;
}
```

Also clear `_waitingForKeyRelease` in `Reset()`:
```csharp
_waitingForKeyRelease = -1;
```

- [ ] **Step 4: Run test**

```bash
dotnet test Chip8Emulator.Tests --filter "OP_Fx0A_DoesNotAdvance"
```
Expected: Pass.

- [ ] **Step 5: Commit**

```bash
git add Chip8Emulator/Core/CPU.cs Chip8Emulator.Tests/CpuTests.cs
git commit -m "fix: Fx0A now correctly waits for key release before advancing"
```

---

## Task 6: Add Shift Quirks Mode

**Files:**
- Modify: `Chip8Emulator/Core/CPU.cs` — `OP_8xy6`, `OP_8xyE`, add `ShiftUsesVy`
- Modify: `Chip8Emulator/Chip8InterfaceOpenGlControl.cs` — wire quirk to CPU
- Modify: `Chip8Emulator/ViewModels/MainWindowViewModel.cs` — add `ShiftQuirksEnabled`
- Modify: `Chip8Emulator/Views/MainWindow.axaml` — add quirks checkbox
- Modify: `Chip8Emulator/Views/MainWindow.axaml.cs` — pass quirk to control
- Modify: `Chip8Emulator.Tests/CpuTests.cs` — add shift tests

- [ ] **Step 1: Write failing tests**

Add to `CpuTests.cs`:
```csharp
[Fact]
public void OP_8xy6_WithShiftUsesVy_False_ShiftsVx()
{
    var cpu = CreateCpu(0x86, 0x16); // SHR V6, (V1 ignored)
    cpu.Registers[6] = 0b00000110;
    cpu.Registers[1] = 0b11110000; // V1 should be ignored
    cpu.ShiftUsesVy = false;
    cpu.Cycle();
    Assert.Equal(0b00000011, cpu.Registers[6]);
    Assert.Equal(0, cpu.Registers[0xF]); // LSB was 0
}

[Fact]
public void OP_8xy6_WithShiftUsesVy_True_CopiesVyThenShifts()
{
    var cpu = CreateCpu(0x86, 0x16); // SHR V6, V1
    cpu.Registers[6] = 0b11110000; // will be overwritten by Vy
    cpu.Registers[1] = 0b00000110; // Vy
    cpu.ShiftUsesVy = true;
    cpu.Cycle();
    Assert.Equal(0b00000011, cpu.Registers[6]); // shifted from V1
    Assert.Equal(0, cpu.Registers[0xF]);
}
```

- [ ] **Step 2: Run to confirm they fail**

```bash
dotnet test Chip8Emulator.Tests --filter "OP_8xy6"
```
Expected: Both fail.

- [ ] **Step 3: Add `ShiftUsesVy` to CPU and update opcodes**

In `CPU.cs`, add a public property near the other public members:
```csharp
public bool ShiftUsesVy { get; set; } = false;
```

Replace `OP_8xy6`:
```csharp
private void OP_8xy6()
{
    byte x = (byte)((Opcode & 0x0F00) >> 8);
    byte y = (byte)((Opcode & 0x00F0) >> 4);
    byte val = ShiftUsesVy ? Registers[y] : Registers[x];
    Registers[0xF] = (byte)(val & 0x1);
    Registers[x] = (byte)(val >> 1);
}
```

Replace `OP_8xyE`:
```csharp
private void OP_8xyE()
{
    byte x = (byte)((Opcode & 0x0F00) >> 8);
    byte y = (byte)((Opcode & 0x00F0) >> 4);
    byte val = ShiftUsesVy ? Registers[y] : Registers[x];
    Registers[0xF] = (byte)((val & 0x80) >> 7);
    Registers[x] = (byte)(val << 1);
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test Chip8Emulator.Tests --filter "OP_8xy6"
```
Expected: Both pass.

- [ ] **Step 5: Add `ShiftQuirksEnabled` to ViewModel**

In `MainWindowViewModel.cs`, add after `ShowOpenGlControl`:
```csharp
public bool ShiftQuirksEnabled
{
    get;
    set
    {
        field = value;
        OnPropertyChanged();
    }
} = false;
```

- [ ] **Step 6: Wire quirk in MainWindow code-behind**

In `MainWindow.axaml.cs`, add a handler that passes the quirk setting to the control. Add to constructor after the timer setup:
```csharp
_viewModel.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(MainWindowViewModel.ShiftQuirksEnabled))
        Chip8OpenGlControl.Cpu.ShiftUsesVy = _viewModel.ShiftQuirksEnabled;
};
```

- [ ] **Step 7: Add quirks checkbox to XAML**

In `MainWindow.axaml`, inside the CONTROLS `StackPanel` (after the two buttons `Grid`), add:
```xml
<CheckBox Content="Shift Quirks (copy Vy)" Foreground="White"
          IsChecked="{Binding ShiftQuirksEnabled}" />
```

- [ ] **Step 8: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 9: Commit**

```bash
git add Chip8Emulator/Core/CPU.cs Chip8Emulator.Tests/CpuTests.cs Chip8Emulator/ViewModels/MainWindowViewModel.cs Chip8Emulator/Views/MainWindow.axaml Chip8Emulator/Views/MainWindow.axaml.cs
git commit -m "feat: add configurable shift quirks mode (8xy6/8xyE Vy copy)"
```

---

## Task 7: Remove Dead Code and Console Logs

**Files:**
- Delete: `Chip8Emulator/CubeRenderingTkOpenGlControl.cs`
- Modify: `Chip8Emulator/Chip8InterfaceOpenGlControl.cs` — remove `Console.WriteLine`
- Modify: `Chip8Emulator/ViewModels/MainWindowViewModel.cs` — remove `PropertyChangedHandler`

- [ ] **Step 1: Delete the dead cube renderer**

```bash
rm C:/Users/yanev/source/repos/Chip8Emulator/Chip8Emulator/CubeRenderingTkOpenGlControl.cs
```

- [ ] **Step 2: Remove Console.WriteLine from Chip8InterfaceOpenGlControl**

In `Chip8InterfaceOpenGlControl.cs`, remove line 43:
```csharp
// Delete this line:
Console.WriteLine("UI: Creating OpenGLControl");
```

- [ ] **Step 3: Remove PropertyChangedHandler from ViewModel**

In `MainWindowViewModel.cs`:
1. Remove the `PropertyChanged += PropertyChangedHandler;` line from the constructor.
2. Delete the entire `PropertyChangedHandler` method (lines 153–163).

- [ ] **Step 4: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: remove dead CubeRenderer, debug Console.WriteLines, and no-op property handler"
```

---

## Task 8: Fix ViewModel Property Notification Inconsistency

**Files:**
- Modify: `Chip8Emulator/ViewModels/MainWindowViewModel.cs`

The six plain auto-properties (`ProgramCounter`, `IndexRegister`, `Opcode`, `StackPointer`, `DelayTimer`, `SoundTimer`) get their `OnPropertyChanged` called manually from `UpdateCpuState`. Consolidate them to use the same setter pattern as `ShowOpenGlControl`.

- [ ] **Step 1: Replace the six auto-properties**

In `MainWindowViewModel.cs`, replace lines 65–76:
```csharp
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
```

- [ ] **Step 2: Simplify UpdateCpuState to use direct assignment**

In `UpdateCpuState`, replace the six `if (...) { ... OnPropertyChanged(...) }` blocks with simple assignments (the setter now fires the notification):
```csharp
public void UpdateCpuState(CPU cpu)
{
    ProgramCounter = cpu.PC.ToString("X4");
    IndexRegister = cpu.I.ToString("X4");
    Opcode = cpu.Opcode.ToString("X4");
    StackPointer = cpu.SP.ToString("X2");
    DelayTimer = cpu.DelayTimer.ToString("X2");
    SoundTimer = cpu.SoundTimer.ToString("X2");

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
```

- [ ] **Step 3: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Chip8Emulator/ViewModels/MainWindowViewModel.cs
git commit -m "refactor: unify ViewModel property notification to setter pattern"
```

---

## Task 9: Fix Timer Progress Bars

**Files:**
- Modify: `Chip8Emulator/ViewModels/MainWindowViewModel.cs` — add `DelayTimerValue`, `SoundTimerValue` as `byte`
- Modify: `Chip8Emulator/Views/MainWindow.axaml` — bind bar width to timer value

The timer indicator bars currently have hardcoded `Width="20"`. We need to bind their width proportionally to the timer value (max 255).

- [ ] **Step 1: Add numeric timer properties to ViewModel**

In `MainWindowViewModel.cs`, add after `SoundTimer`:
```csharp
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
```

Update `UpdateCpuState` to also set these:
```csharp
DelayTimerValue = cpu.DelayTimer;
SoundTimerValue = cpu.SoundTimer;
```

- [ ] **Step 2: Update the XAML timer bars**

In `MainWindow.axaml`, find the Delay Timer `Border` block (line ~110). Replace the static inner bar:
```xml
<!-- Before -->
<Border Background="#444" Height="2" Margin="0,5,0,0" CornerRadius="1">
    <Border Background="#FFF" HorizontalAlignment="Left" Width="20" />
</Border>

<!-- After (Delay Timer) -->
<Border Background="#444" Height="4" Margin="0,5,0,0" CornerRadius="2">
    <Border Background="#4CAF50" HorizontalAlignment="Left" CornerRadius="2"
            Width="{Binding DelayTimerValue,
                   Converter={x:Static conv:TimerWidthConverter.Instance}}" />
</Border>
```

Do the same for Sound Timer (line ~128), using `SoundTimerValue` and `Background="#FF5722"`.

- [ ] **Step 3: Add the converter**

Create `Chip8Emulator/Converters/TimerWidthConverter.cs`:
```csharp
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Chip8Emulator.Converters;

public class TimerWidthConverter : IValueConverter
{
    public static readonly TimerWidthConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte b)
            return (double)b / 255.0 * 160.0; // 160px max bar width
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 4: Register the namespace in XAML**

In `MainWindow.axaml`, add to the namespace declarations at the top:
```xml
xmlns:conv="using:Chip8Emulator.Converters"
```

- [ ] **Step 5: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add Chip8Emulator/ViewModels/MainWindowViewModel.cs Chip8Emulator/Views/MainWindow.axaml Chip8Emulator/Converters/
git commit -m "fix: timer progress bars now reflect actual delay/sound timer values"
```

---

## Task 10: Add Speed Control

**Files:**
- Modify: `Chip8Emulator/Chip8InterfaceOpenGlControl.cs` — make frequency runtime-settable
- Modify: `Chip8Emulator/ViewModels/MainWindowViewModel.cs` — add `CpuFrequency`
- Modify: `Chip8Emulator/Views/MainWindow.axaml` — add speed slider
- Modify: `Chip8Emulator/Views/MainWindow.axaml.cs` — wire slider to control

- [ ] **Step 1: Make frequency runtime-settable in the control**

In `Chip8InterfaceOpenGlControl.cs`, replace the compile-time constants with a runtime property:
```csharp
// Remove these constants:
// private const double CpuFrequency = 700.0;
// private const double CpuPeriod = 1000.0 / CpuFrequency;

private const double TimerFrequency = 60.0;
private const double TimerPeriod = 1000.0 / TimerFrequency;

private double _cpuFrequency = 700.0;

public double CpuFrequency
{
    get => _cpuFrequency;
    set => _cpuFrequency = Math.Clamp(value, 60.0, 2000.0);
}

private double CpuPeriod => 1000.0 / _cpuFrequency;
```

- [ ] **Step 2: Add `CpuFrequency` to ViewModel**

In `MainWindowViewModel.cs`, add:
```csharp
public double CpuFrequency
{
    get;
    set { field = value; OnPropertyChanged(); }
} = 700.0;
```

- [ ] **Step 3: Wire ViewModel → control in MainWindow.axaml.cs**

In the `PropertyChanged` handler added in Task 6, add a case:
```csharp
_viewModel.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(MainWindowViewModel.ShiftQuirksEnabled))
        Chip8OpenGlControl.Cpu.ShiftUsesVy = _viewModel.ShiftQuirksEnabled;
    if (e.PropertyName == nameof(MainWindowViewModel.CpuFrequency))
        Chip8OpenGlControl.CpuFrequency = _viewModel.CpuFrequency;
};
```

- [ ] **Step 4: Add speed slider to XAML**

In `MainWindow.axaml`, inside the CONTROLS `StackPanel`, after the quirks checkbox, add:
```xml
<StackPanel Spacing="4">
    <Grid ColumnDefinitions="*,Auto">
        <TextBlock Text="CPU SPEED" Foreground="#888" FontWeight="Bold" FontSize="12"
                   VerticalAlignment="Center" />
        <TextBlock Grid.Column="1" Foreground="#DDD" FontFamily="Consolas, Monospace" FontSize="12">
            <Run Text="{Binding CpuFrequency, StringFormat={}{0:F0}}" />
            <Run Text=" Hz" />
        </TextBlock>
    </Grid>
    <Slider Minimum="60" Maximum="2000" Value="{Binding CpuFrequency}"
            TickFrequency="100" IsSnapToTickEnabled="False" />
</StackPanel>
```

- [ ] **Step 5: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add Chip8Emulator/Chip8InterfaceOpenGlControl.cs Chip8Emulator/ViewModels/MainWindowViewModel.cs Chip8Emulator/Views/MainWindow.axaml Chip8Emulator/Views/MainWindow.axaml.cs
git commit -m "feat: add runtime CPU speed control slider (60–2000 Hz)"
```

---

## Task 11: Add Pause and Reset

**Files:**
- Modify: `Chip8Emulator/Chip8InterfaceOpenGlControl.cs` — add `IsPaused`, `Reset()`
- Modify: `Chip8Emulator/Core/CPU.cs` — make `Reset()` public
- Modify: `Chip8Emulator/ViewModels/MainWindowViewModel.cs` — add `IsPaused`, `CanPause`
- Modify: `Chip8Emulator/Views/MainWindow.axaml` — add Pause/Reset buttons
- Modify: `Chip8Emulator/Views/MainWindow.axaml.cs` — click handlers

- [ ] **Step 1: Make CPU.Reset() public and track last ROM path**

In `CPU.cs`, change `private void Reset()` to `public void Reset()`.

In `Chip8InterfaceOpenGlControl.cs`, add a field and `Reset()` method:
```csharp
private string? _lastRomPath;

public bool IsPaused { get; set; }

public void LoadRom(string path)
{
    _lastRomPath = path;
    Cpu.LoadROM(path);
    IsRomLoaded = true;
    IsPaused = false;
}

public void ResetEmulator()
{
    if (_lastRomPath != null)
        Cpu.LoadROM(_lastRomPath);
    else
        Cpu.Reset();
    IsPaused = false;
}
```

- [ ] **Step 2: Skip emulation loop when paused**

In `Chip8InterfaceOpenGlControl.cs`, in `OpenTkRender()`, wrap the emulation section:
```csharp
protected override void OpenTkRender()
{
    if (!IsRomLoaded) return;

    ProcessInput();

    if (!IsPaused)
    {
        long currentTime = stopwatch.ElapsedMilliseconds;
        double deltaTime = currentTime - lastCycleTime;
        lastCycleTime = currentTime;
        if (deltaTime > 100) deltaTime = 100;

        _cpuAccumulator += deltaTime;
        _timerAccumulator += deltaTime;

        while (_cpuAccumulator >= CpuPeriod)
        {
            Cpu.Cycle();
            _cpuAccumulator -= CpuPeriod;
        }

        while (_timerAccumulator >= TimerPeriod)
        {
            Cpu.UpdateTimers();
            _timerAccumulator -= TimerPeriod;
        }

        if (Cpu.SoundTimer > 0) _soundPlayer?.Play();
        else _soundPlayer?.Stop();
    }

    // Rendering always happens (so display doesn't freeze)
    GL.Clear(ClearBufferMask.ColorBufferBit);
    GL.UseProgram(shaderProgram);
    GL.BindTexture(TextureTarget.Texture2D, textureHandle);
    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
        64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Cpu.video);
    GL.BindVertexArray(vaoHandle);
    GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
}
```

- [ ] **Step 3: Add ViewModel properties**

In `MainWindowViewModel.cs`, add:
```csharp
public bool IsPaused
{
    get;
    set
    {
        field = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(PauseButtonText));
    }
}

public string PauseButtonText => IsPaused ? "RESUME" : "PAUSE";
```

- [ ] **Step 4: Add Pause/Reset buttons to XAML**

In `MainWindow.axaml`, in the CONTROLS `StackPanel`, replace the existing two-button `Grid` with a three-column version:
```xml
<Grid ColumnDefinitions="*,*,*">
    <Button Grid.Column="0" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"
            Click="LoadRom_Click" Background="#444" Foreground="White" Margin="0,0,3,0">
        LOAD ROM
    </Button>
    <Button Grid.Column="1" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"
            Click="Pause_Click" Background="#444" Foreground="White" Margin="3,0,3,0"
            Content="{Binding PauseButtonText}" />
    <Button Grid.Column="2" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"
            Click="Reset_Click" Background="#444" Foreground="White" Margin="3,0,0,0">
        RESET
    </Button>
</Grid>
```

Remove the old TOGGLE GPU button (it's replaced; if still needed, add it back in a second row).

- [ ] **Step 5: Add click handlers to MainWindow.axaml.cs**

```csharp
private void Pause_Click(object? sender, RoutedEventArgs e)
{
    _viewModel.IsPaused = !_viewModel.IsPaused;
    Chip8OpenGlControl.IsPaused = _viewModel.IsPaused;
}

private void Reset_Click(object? sender, RoutedEventArgs e)
{
    Chip8OpenGlControl.ResetEmulator();
    _viewModel.IsPaused = false;
}
```

Also update `LoadRom_Click` to reset `IsPaused`:
```csharp
// At the end of LoadRom_Click, after LoadRom:
_viewModel.IsPaused = false;
```

- [ ] **Step 6: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add Chip8Emulator/Core/CPU.cs Chip8Emulator/Chip8InterfaceOpenGlControl.cs Chip8Emulator/ViewModels/MainWindowViewModel.cs Chip8Emulator/Views/MainWindow.axaml Chip8Emulator/Views/MainWindow.axaml.cs
git commit -m "feat: add pause/resume and reset controls"
```

---

## Task 12: Add Color Palette Selection

**Files:**
- Modify: `Chip8Emulator/Chip8InterfaceOpenGlControl.cs` — shader uniforms for fg/bg color
- Modify: `Chip8Emulator/ViewModels/MainWindowViewModel.cs` — palette selection
- Modify: `Chip8Emulator/Views/MainWindow.axaml` — palette combo box
- Modify: `Chip8Emulator/Views/MainWindow.axaml.cs` — wire palette change

- [ ] **Step 1: Add color uniforms to the shader**

In `Chip8InterfaceOpenGlControl.cs`, update the fragment shader source in `OpenTkInit`:
```csharp
const string fragmentShaderSource = """
    #version 330 core
    out vec4 FragColor;
    in vec2 TexCoord;
    uniform sampler2D texture1;
    uniform vec4 uForeground;
    uniform vec4 uBackground;
    void main() {
        float pixel = texture(texture1, TexCoord).r;
        FragColor = mix(uBackground, uForeground, step(0.5, pixel));
    }
    """;
```

- [ ] **Step 2: Cache uniform locations and add color properties**

In `Chip8InterfaceOpenGlControl.cs`, add fields:
```csharp
private int _uForeground;
private int _uBackground;

// RGBA packed as floats: R, G, B, A each 0.0–1.0
public (float R, float G, float B) ForegroundColor { get; set; } = (1f, 1f, 1f);
public (float R, float G, float B) BackgroundColor { get; set; } = (0f, 0f, 0f);
```

In `OpenTkInit`, after `GL.LinkProgram(shaderProgram)`:
```csharp
_uForeground = GL.GetUniformLocation(shaderProgram, "uForeground");
_uBackground = GL.GetUniformLocation(shaderProgram, "uBackground");
```

In `OpenTkRender`, after `GL.UseProgram(shaderProgram)`:
```csharp
GL.Uniform4(_uForeground, ForegroundColor.R, ForegroundColor.G, ForegroundColor.B, 1.0f);
GL.Uniform4(_uBackground, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, 1.0f);
```

- [ ] **Step 3: Define palette model in ViewModel**

In `MainWindowViewModel.cs`, add a nested record and palette list:
```csharp
public record ColorPalette(string Name,
    float FgR, float FgG, float FgB,
    float BgR, float BgG, float BgB);

public List<ColorPalette> Palettes { get; } =
[
    new("Classic B&W",    1.0f, 1.0f, 1.0f,  0.0f, 0.0f, 0.0f),
    new("Green Phosphor", 0.0f, 1.0f, 0.2f,  0.0f, 0.1f, 0.0f),
    new("Amber",          1.0f, 0.7f, 0.0f,  0.1f, 0.05f, 0.0f),
    new("Blue LCD",       0.4f, 0.8f, 1.0f,  0.0f, 0.05f, 0.15f),
    new("Inverted",       0.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f),
];

public ColorPalette SelectedPalette
{
    get;
    set { field = value; OnPropertyChanged(); }
} = null!;

public MainWindowViewModel()
{
    // ... existing code ...
    SelectedPalette = Palettes[0];
}
```

- [ ] **Step 4: Wire palette selection in MainWindow.axaml.cs**

In the `PropertyChanged` handler, add:
```csharp
if (e.PropertyName == nameof(MainWindowViewModel.SelectedPalette))
{
    var p = _viewModel.SelectedPalette;
    Chip8OpenGlControl.ForegroundColor = (p.FgR, p.FgG, p.FgB);
    Chip8OpenGlControl.BackgroundColor = (p.BgR, p.BgG, p.BgB);
}
```

- [ ] **Step 5: Add palette combo box to XAML**

In `MainWindow.axaml`, in the CONTROLS `StackPanel`, add after the speed slider:
```xml
<StackPanel Spacing="4">
    <TextBlock Text="COLOR PALETTE" Foreground="#888" FontWeight="Bold" FontSize="12" />
    <ComboBox ItemsSource="{Binding Palettes}"
              SelectedItem="{Binding SelectedPalette}"
              HorizontalAlignment="Stretch"
              Background="#444" Foreground="White">
        <ComboBox.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding Name}" />
            </DataTemplate>
        </ComboBox.ItemTemplate>
    </ComboBox>
</StackPanel>
```

- [ ] **Step 6: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add Chip8Emulator/Chip8InterfaceOpenGlControl.cs Chip8Emulator/ViewModels/MainWindowViewModel.cs Chip8Emulator/Views/MainWindow.axaml Chip8Emulator/Views/MainWindow.axaml.cs
git commit -m "feat: add color palette selection (B&W, phosphor, amber, LCD, inverted)"
```

---

## Task 13: Preserve Display Aspect Ratio

**Files:**
- Modify: `Chip8Emulator/Views/MainWindow.axaml` — wrap GL control in aspect-ratio container

The Chip-8 display is 64×32 (2:1). Currently the GL panel stretches to fill arbitrary sizes. We wrap it in a container that enforces 2:1.

- [ ] **Step 1: Replace the Panel with a Viewbox-constrained container**

In `MainWindow.axaml`, replace:
```xml
<Panel Grid.Column="2" IsVisible="{Binding ShowOpenGlControl}">
    <chip8Emulator:Chip8InterfaceOpenGlControl x:Name="Chip8OpenGlControl" Focusable="True" />
</Panel>
```

With:
```xml
<Panel Grid.Column="2" IsVisible="{Binding ShowOpenGlControl}"
       Background="#111">
    <Viewbox Stretch="Uniform" HorizontalAlignment="Center" VerticalAlignment="Center">
        <chip8Emulator:Chip8InterfaceOpenGlControl
            x:Name="Chip8OpenGlControl"
            Focusable="True"
            Width="640"
            Height="320" />
    </Viewbox>
</Panel>
```

This forces a 2:1 (640×320) intrinsic size, and `Viewbox Stretch="Uniform"` scales it to fit the available space while preserving the ratio.

- [ ] **Step 2: Build**

```bash
cd Chip8Emulator/Chip8Emulator && dotnet build
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Chip8Emulator/Views/MainWindow.axaml
git commit -m "fix: preserve 2:1 aspect ratio on Chip-8 display using Viewbox"
```

---

## Self-Review

**Spec coverage check:**

| Improvement | Task |
|-------------|------|
| CPU clock speed bug | Task 1 ✓ |
| Sound wrong pitch | Task 2 ✓ |
| Stack bounds | Task 4 ✓ |
| Fx0A key release | Task 5 ✓ |
| Shift quirks | Task 6 ✓ |
| Dead code removal | Task 7 ✓ |
| ViewModel consistency | Task 8 ✓ |
| Remove debug handler | Task 7 ✓ |
| Timer progress bars | Task 9 ✓ |
| Speed control | Task 10 ✓ |
| Pause/Reset | Task 11 ✓ |
| Color palette | Task 12 ✓ |
| Aspect ratio | Task 13 ✓ |

**Type consistency check:** `ColorPalette` record used in Task 12 ViewModel and XAML binding — consistent. `CpuFrequency` property defined in Task 10, wired in same task — consistent. `ShiftUsesVy` on `CPU` defined in Task 6, used in same task — consistent.

**No placeholders found.**
