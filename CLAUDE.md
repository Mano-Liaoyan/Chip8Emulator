# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

Requires .NET 10 SDK.

```sh
# Run the app
cd Chip8Emulator/Chip8Emulator && dotnet run

# Build only
dotnet build Chip8Emulator.sln

# Publish AOT single-file (Windows)
dotnet publish Chip8Emulator/Chip8Emulator -c Release
```

There are no automated tests in this project.

## Architecture

The solution has two projects:

**`OpenTKAvalonia/`** — A reusable library that bridges Avalonia UI's OpenGL control (`OpenGlControlBase`) with OpenTK's GL bindings. `BaseTkOpenGlControl` is the abstract base class providing `OpenTkInit`, `OpenTkRender`, and `OpenTkTeardown` lifecycle hooks, plus `AvaloniaKeyboardState` for stateful key tracking. Subclasses override these three methods.

**`Chip8Emulator/`** — The main application:

- `Core/CPU.cs` — The full Chip-8 interpreter. 4KB memory, 16 registers (V0–VF), 16-level stack. Opcode dispatch uses a jump table (`table[]` indexed by first nibble, with sub-tables `table0`, `table8`, `tableE`, `tableF`). `Cycle()` fetches+decodes+executes one instruction. `UpdateTimers()` decrements delay/sound timers and must be called at 60 Hz separately from `Cycle()`.

- `Chip8InterfaceOpenGlControl.cs` — Extends `BaseTkOpenGlControl`. Owns the `CPU` instance and drives the emulation loop inside `OpenTkRender()` using a fixed-timestep accumulator: CPU runs at 700 Hz (`CpuPeriod`), timers at 60 Hz (`TimerPeriod`). Renders the 64×32 `cpu.video` buffer as a GL texture uploaded each frame. Handles keyboard input via `ProcessInput()` mapping QWERTY to Chip-8 keypad. Sound is played via `Core/SoundPlayer.cs` when `SoundTimer > 0`.

- `Views/MainWindow.axaml.cs` — Creates a `DispatcherTimer` at 30 ms intervals to call `_viewModel.UpdateCpuState(cpu)`, keeping the debug panel in sync with emulator state.

- `ViewModels/MainWindowViewModel.cs` — Holds observable collections for registers (V0–VF), keypad state, and stack. `UpdateCpuState()` is called from the UI thread to refresh all CPU state displays.

## Key Design Notes

- The emulation loop runs entirely inside `OpenTkRender()` (called every frame by Avalonia's render scheduler), not on a separate thread. `Dispatcher.UIThread.Post(RequestNextFrameRendering)` at the end of each render schedules the next frame.
- `PublishAot=true` is set in the csproj — AOT-incompatible patterns will cause publish failures even if debug builds succeed.
- On Linux/macOS, there is a whitelist bypass for the llvmpipe software renderer restriction (see recent commit history).
- ROM files use `.ch8` extension and are loaded into memory starting at address `0x200`. The `roms/` directory contains bundled test ROMs.
