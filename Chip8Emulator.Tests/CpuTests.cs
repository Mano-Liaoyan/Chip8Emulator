using System;
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

    [Fact]
    public void OP_2nnn_AtMaxStack_ThrowsInvalidOperationException()
    {
        // 0x2200 = CALL 0x200 — self-referential, so SP grows by 1 each cycle
        // 16 cycles fill the stack; the 17th must throw
        var cpu = CreateCpu(0x22, 0x00);
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

    [Fact]
    public void OP_Fx0A_DoesNotAdvance_UntilKeyReleased()
    {
        // 0xF00A = wait for key, store in V0
        var cpu = CreateCpu(0xF0, 0x0A);

        // Simulate key 3 pressed — should NOT advance yet (key still held)
        cpu.SetKey(3, true);
        cpu.Cycle();
        Assert.Equal(0x200, cpu.PC); // re-executed (PC decremented back)

        // Key still held — must still not advance
        cpu.Cycle();
        Assert.Equal(0x200, cpu.PC);

        // Release key 3 — now it should store and advance
        cpu.SetKey(3, false);
        cpu.Cycle();
        Assert.Equal(0x202, cpu.PC);
        Assert.Equal(3, cpu.Registers[0]);
    }

    [Fact]
    public void OP_Fx0A_DoesNotAdvance_WhenNoKeyPressed()
    {
        var cpu = CreateCpu(0xF0, 0x0A);

        // No keys pressed — must stay on this instruction across multiple cycles
        cpu.Cycle();
        Assert.Equal(0x200, cpu.PC);
        cpu.Cycle();
        Assert.Equal(0x200, cpu.PC);
        cpu.Cycle();
        Assert.Equal(0x200, cpu.PC);
    }

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

    [Fact]
    public void OP_8xyE_WithShiftUsesVy_True_CopiesVyThenShifts()
    {
        var cpu = CreateCpu(0x86, 0x1E); // SHL V6, V1
        cpu.Registers[6] = 0b11110000;
        cpu.Registers[1] = 0b10000001; // Vy: MSB=1, will be shifted out
        cpu.ShiftUsesVy = true;
        cpu.Cycle();
        Assert.Equal(0b00000010, cpu.Registers[6]);
        Assert.Equal(1, cpu.Registers[0xF]); // MSB was 1
    }
}
