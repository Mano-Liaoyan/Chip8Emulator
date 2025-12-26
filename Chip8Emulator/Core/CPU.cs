using System;
using System.IO;

namespace Chip8Emulator.Core;

public class CPU
{
    public byte[] Registers = new byte[16];

    private byte[] memory = new byte[4096];
    // Index Register
    public ushort Ir;
    // Program Counter
    public ushort Pc;
    public ushort[] Stack = new ushort[16];
    // Stack Pointer
    public byte Sp;
    public byte DelayTimer;
    public byte SoundTimer;
    public byte[] Keypad = new byte[16];
    public uint[] Video = new uint[64 * 32];
    public ushort Opcode;

    private const ushort startAddress = 0x200;
    private const ushort fontsetStartAddress = 0x50;
    
    private Random _random;
    
    private static readonly byte[] fontset =
    [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    ];

    public CPU()
    {
        Pc = startAddress;
        _random = new Random();
        for (ushort i = 0; i < fontset.Length; ++i)
        {
            memory[fontsetStartAddress + i] = fontset[i];
        }
    }
    


    public void LoadROM(string romPath)
    {
        if (File.Exists(romPath))
        {
            byte[] rom = File.ReadAllBytes(romPath);
            
            for (int i = 0; i < rom.Length && (startAddress + i) < memory.Length; i++)
            {
                memory[startAddress + i] = rom[i];
            }
        }
        else
        {
            throw new FileNotFoundException($"ROM file not found: {romPath}");
        }
    }
    
    private byte GetRandomByte()
    {
        return (byte)_random.Next(0, 256);
    }
}