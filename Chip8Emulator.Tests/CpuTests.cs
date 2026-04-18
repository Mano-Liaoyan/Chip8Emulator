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
}
