using System;
using System.Collections.Generic;
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
        var cpu = new CPU();
        // CALL 0x200 — calls itself, filling the stack. ROM is 2 bytes: 0x22 0x00 = CALL 0x200
        var rom = new List<byte>();
        for (int i = 0; i < 16; i++) { rom.Add(0x22); rom.Add(0x00); } // 16x CALL 0x200
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
}
