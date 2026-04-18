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
}
