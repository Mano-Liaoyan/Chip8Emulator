using System;
using System.IO;

namespace Chip8Emulator.Core;

public class CPU
{
    private const ushort startAddress = 0x200;
    private const ushort fontsetStartAddress = 0x50;

    private const ushort VIDEO_WIDTH = 64;
    private const ushort VIDEO_HEIGHT = 32;

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
        0xF0, 0x80, 0xF0, 0x80, 0x80 // F
    ];

    private readonly Random _random;

    public readonly byte[] Keypad = new byte[16];

    private readonly byte[] memory = new byte[4096];

    public readonly byte[] Registers = new byte[16];

    /// <summary>
    ///     The stack
    /// </summary>
    public readonly ushort[] Stack = new ushort[16];

    private readonly Chip8Instruction[] table;
    private readonly Chip8Instruction[] table0;
    private readonly Chip8Instruction[] table8;
    private readonly Chip8Instruction[] tableE;
    private readonly Chip8Instruction[] tableF;

    public readonly uint[] video = new uint[VIDEO_WIDTH * VIDEO_HEIGHT];

    public void SetKey(int index, bool pressed)
    {
        Keypad[index] = (byte)(pressed ? 1 : 0);
    }

    public CPU()
    {
        _random = new Random();

        // Initialize the arrays
        table = new Chip8Instruction[0xF + 1];
        table0 = new Chip8Instruction[0xE + 1];
        table8 = new Chip8Instruction[0xE + 1];
        tableE = new Chip8Instruction[0xE + 1];
        tableF = new Chip8Instruction[0x65 + 1];

        table[0x0] = Table0;
        table[0x1] = OP_1nnn;
        table[0x2] = OP_2nnn;
        table[0x3] = OP_3xkk;
        table[0x4] = OP_4xkk;
        table[0x5] = OP_5xy0;
        table[0x6] = OP_6xkk;
        table[0x7] = OP_7xkk;
        table[0x8] = Table8;
        table[0x9] = OP_9xy0;
        table[0xA] = OP_Annn;
        table[0xB] = OP_Bnnn;
        table[0xC] = OP_Cxkk;
        table[0xD] = OP_Dxyn;
        table[0xE] = TableE;
        table[0xF] = TableF;

        for (int i = 0; i < table0.Length; i++) table0[i] = OP_NULL;
        for (int i = 0; i < table8.Length; i++) table8[i] = OP_NULL;
        for (int i = 0; i < tableE.Length; i++) tableE[i] = OP_NULL;
        for (int i = 0; i < tableF.Length; i++) tableF[i] = OP_NULL;

        table0[0x0] = OP_00E0;
        table0[0xE] = OP_00EE;

        table8[0x0] = OP_8xy0;
        table8[0x1] = OP_8xy1;
        table8[0x2] = OP_8xy2;
        table8[0x3] = OP_8xy3;
        table8[0x4] = OP_8xy4;
        table8[0x5] = OP_8xy5;
        table8[0x6] = OP_8xy6;
        table8[0x7] = OP_8xy7;
        table8[0xE] = OP_8xyE;

        tableE[0x1] = OP_ExA1;
        tableE[0xE] = OP_Ex9E;

        tableF[0x07] = OP_Fx07;
        tableF[0x0A] = OP_Fx0A;
        tableF[0x15] = OP_Fx15;
        tableF[0x18] = OP_Fx18;
        tableF[0x1E] = OP_Fx1E;
        tableF[0x29] = OP_Fx29;
        tableF[0x33] = OP_Fx33;
        tableF[0x55] = OP_Fx55;
        tableF[0x65] = OP_Fx65;

        Reset();
    }

    public byte DelayTimer { get; private set; }

    /// <summary>
    ///     Index Register
    /// </summary>
    public ushort I { get; private set; }

    public ushort Opcode { get; private set; }

    /// <summary>
    ///     Program Counter
    /// </summary>
    public ushort PC { get; private set; }

    public byte SoundTimer { get; private set; }

    /// <summary>
    ///     Stack Pointer
    /// </summary>
    public byte SP { get; private set; }

    private void Reset()
    {
        PC = startAddress;
        Opcode = 0;
        I = 0;
        SP = 0;
        DelayTimer = 3;
        SoundTimer = 0;

        Array.Clear(memory, 0, memory.Length);
        Array.Clear(Registers, 0, Registers.Length);
        Array.Clear(Stack, 0, Stack.Length);
        Array.Clear(video, 0, video.Length);
        Array.Clear(Keypad, 0, Keypad.Length);

        for (ushort i = 0; i < fontset.Length; ++i) memory[fontsetStartAddress + i] = fontset[i];
    }

    // Helper Table methods
    private void Table0()
    {
        table0[Opcode & 0x000F]();
    }

    private void Table8()
    {
        table8[Opcode & 0x000F]();
    }

    private void TableE()
    {
        tableE[Opcode & 0x000F]();
    }

    private void TableF()
    {
        tableF[Opcode & 0x00FF]();
    }

    private void OP_NULL()
    {
        /* Do nothing */
    }

    public void Cycle()
    {
        // Fetch: Read 2 bytes from memory and combine them into one 16-bit opcode
        Opcode = (ushort)((memory[PC] << 8) | memory[PC + 1]);

        // Move PC forward before executing
        PC += 2;

        // Decode & Execute: Use the first nibble to jump into the table
        table[(Opcode & 0xF000) >> 12]();

        // Decrement the delay timer if it's been set
        if (DelayTimer > 0) DelayTimer--;

        // Decrement the sound timer if it's been set
        if (SoundTimer > 0) SoundTimer--;
    }

    public void LoadROM(string romPath)
    {
        Reset();
        if (File.Exists(romPath))
        {
            byte[] rom = File.ReadAllBytes(romPath);

            for (int i = 0; i < rom.Length && startAddress + i < memory.Length; i++) memory[startAddress + i] = rom[i];
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

    /// <summary>
    ///     Clear the display
    /// </summary>
    private void OP_00E0()
    {
        Array.Clear(video, 0, video.Length);
    }

    /// <summary>
    ///     Return from a subroutine
    /// </summary>
    private void OP_00EE()
    {
        SP -= 1;
        PC = Stack[SP];
    }

    /// <summary>
    ///     JP address: Jump to location nnn
    /// </summary>
    private void OP_1nnn()
    {
        // Extract the lowest 12 bits from the 16-bit opcode
        ushort address = (ushort)(Opcode & 0x0FFF);

        PC = address;
    }

    /// <summary>
    ///     Call subroutine at nnn
    /// </summary>
    private void OP_2nnn()
    {
        // Extract the lowest 12 bits from the 16-bit opcode
        ushort address = (ushort)(Opcode & 0x0FFF);

        Stack[SP] = PC;
        SP += 1;
        PC = address;
    }

    /// <summary>
    ///     Skip next instruction if Vx = kk
    /// </summary>
    private void OP_3xkk()
    {
        // Extract 'x' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);

        // Extract 'kk' (the 8-bit constant) from the last two nibbles
        byte kk = (byte)(Opcode & 0x00FF);

        if (Registers[x] == kk)
            // Skip the next 2-byte instruction
            PC += 2;
    }

    /// <summary>
    ///     Skip next instruction if Vx != kk
    /// </summary>
    private void OP_4xkk()
    {
        // Extract 'x' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);

        // Extract 'kk' (the 8-bit constant) from the last two nibbles
        byte kk = (byte)(Opcode & 0x00FF);

        if (Registers[x] != kk)
            // Skip the next 2-byte instruction
            PC += 2;
    }

    /// <summary>
    ///     Skip next instruction if Vx = Vy
    /// </summary>
    private void OP_5xy0()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        if (Registers[x] == Registers[y])
            // Skip the next 2-byte instruction
            PC += 2;
    }

    /// <summary>
    ///     Set Vx = kk
    /// </summary>
    private void OP_6xkk()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte kk = (byte)(Opcode & 0x00FF);

        Registers[x] = kk;
    }

    /// <summary>
    ///     Set Vx = Vx + kk
    /// </summary>
    private void OP_7xkk()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte kk = (byte)(Opcode & 0x00FF);

        Registers[x] += kk;
    }

    /// <summary>
    ///     Set Vx = Vy
    /// </summary>
    private void OP_8xy0()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        Registers[x] = Registers[y];
    }

    /// <summary>
    ///     Set Vx = Vx OR Vy
    /// </summary>
    private void OP_8xy1()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        Registers[x] |= Registers[y];
    }

    /// <summary>
    ///     Set Vx = Vx AND Vy
    /// </summary>
    private void OP_8xy2()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        Registers[x] &= Registers[y];
    }

    /// <summary>
    ///     Set Vx = Vx XOR Vy
    /// </summary>
    private void OP_8xy3()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        Registers[x] ^= Registers[y];
    }

    /// <summary>
    ///     Set Vx = Vx + Vy, set VF = carry
    /// </summary>
    private void OP_8xy4()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        int sum = Registers[x] + Registers[y];

        if (sum > 255)
            Registers[0xF] = 1;
        else
            Registers[0xF] = 0;

        Registers[x] = (byte)(sum & 0xFF);
    }

    /// <summary>
    ///     Set Vx = Vx - Vy, set VF = NOT borrow
    /// </summary>
    private void OP_8xy5()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        if (Registers[x] > Registers[y])
            Registers[0xF] = 1;
        else
            Registers[0xF] = 0;

        Registers[x] -= Registers[y];
    }

    /// <summary>
    ///     Set Vx = Vx SHR 1
    /// </summary>
    private void OP_8xy6()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);

        // Save LSB in VF
        Registers[0xF] = (byte)(Registers[x] & 0x1);

        Registers[x] >>= 1;
    }

    /// <summary>
    ///     Set Vx = Vy - Vx, set VF = NOT borrow
    /// </summary>
    private void OP_8xy7()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        if (Registers[y] > Registers[x])
            Registers[0xF] = 1;
        else
            Registers[0xF] = 0;

        Registers[x] = (byte)(Registers[y] - Registers[x]);
    }

    /// <summary>
    ///     Set Vx = Vx SHL 1
    /// </summary>
    private void OP_8xyE()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);

        // Save the Most Significant Bit (MSB) in VF
        // 0x80 is 10000000 in binary. We mask it and shift it 7 places right 
        // to get a simple 0 or 1.
        Registers[0xF] = (byte)((Registers[x] & 0x80) >> 7);

        Registers[x] <<= 1;
    }

    /// <summary>
    ///     Skip next instruction if Vx != Vy
    /// </summary>
    private void OP_9xy0()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);

        if (Registers[x] != Registers[y])
            // Skip the next 2-byte instruction
            PC += 2;
    }

    /// <summary>
    ///     Set I = nnn
    /// </summary>
    private void OP_Annn()
    {
        ushort address = (ushort)(Opcode & 0x0FFF);

        I = address;
    }

    /// <summary>
    ///     Set I = nnn
    /// </summary>
    private void OP_Bnnn()
    {
        ushort address = (ushort)(Opcode & 0x0FFF);

        PC = (ushort)(Registers[0] + address);
    }

    /// <summary>
    ///     Set Vx = random byte AND kk
    /// </summary>
    private void OP_Cxkk()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte kk = (byte)(Opcode & 0x00FF);

        Registers[x] = (byte)(GetRandomByte() & kk);
    }

    /// <summary>
    ///     Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision
    /// </summary>
    private void OP_Dxyn()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte y = (byte)((Opcode & 0x00F0) >> 4);
        byte height = (byte)(Opcode & 0x000F);

        byte xPositon = (byte)(Registers[x] % VIDEO_WIDTH);
        byte yPositon = (byte)(Registers[y] % VIDEO_HEIGHT);

        Registers[0xF] = 0;

        for (ushort row = 0; row < height; ++row)
        {
            byte spriteByte = memory[I + row];

            for (ushort col = 0; col < 8; ++col)
            {
                // Use a mask to see if the specific bit (pixel) in the byte is 1
                // 0x80 >> col shifts the mask (10000000) to the right
                byte spritePixel = (byte)(spriteByte & (0x80 >> col));

                if (spritePixel == 0) continue;

                // Ensure we don't draw outside the screen array bounds
                if (yPositon + row >= VIDEO_HEIGHT || xPositon + col >= VIDEO_WIDTH) continue;


                int screenIndex = (yPositon + row) * VIDEO_WIDTH + xPositon + col;

                // Screen pixel is already on (0xFFFFFFFF) - Collision!
                if (video[screenIndex] == 0xFFFFFFFF) Registers[0xF] = 1;

                // XOR the screen pixel (Invert it)
                video[screenIndex] ^= 0xFFFFFFFF;
            }
        }
    }

    /// <summary>
    ///     Skip next instruction if key with the value of Vx is pressed
    /// </summary>
    private void OP_Ex9E()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte key = Registers[x];

        if (Keypad[key] != 0)
            PC += 2;
    }

    /// <summary>
    ///     Skip next instruction if key with the value of Vx is not pressed
    /// </summary>
    private void OP_ExA1()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte key = Registers[x];

        if (Keypad[key] == 0)
            PC += 2;
    }

    /// <summary>
    ///     Set Vx = delay timer value
    /// </summary>
    private void OP_Fx07()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        Registers[x] = DelayTimer;
    }

    /// <summary>
    ///     Wait for a key press, store the value of the key in Vx
    /// </summary>
    private void OP_Fx0A()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        bool keyPressed = false;

        for (byte i = 0; i < 16; i++)
            if (Keypad[i] != 0)
            {
                Registers[x] = i;
                keyPressed = true;
                break;
            }

        // If no key was pressed, move the PC back 2 bytes.
        // This causes the emulator to run this same instruction again on the next cycle.
        if (!keyPressed) PC -= 2;
    }

    /// <summary>
    ///     Set delay timer = Vx
    /// </summary>
    private void OP_Fx15()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        DelayTimer = Registers[x];
    }

    /// <summary>
    ///     Set sound timer = Vx
    /// </summary>
    private void OP_Fx18()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        SoundTimer = Registers[x];
    }

    /// <summary>
    ///     Set I = I + Vx
    /// </summary>
    private void OP_Fx1E()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        I += Registers[x];
    }

    /// <summary>
    ///     Set I = location of sprite for digit Vx
    /// </summary>
    private void OP_Fx29()
    {
        // Extract 'x' and 'y' (the register index) from the second nibble and shift it to the end
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        // We know the font characters are located at 0x50, and we know they’re five bytes each, so we can get the
        // address of the first byte of any character by taking an offset from the start address.
        I = (ushort)(fontsetStartAddress + 5 * Registers[x]);
    }

    /// <summary>
    ///     Store BCD representation of Vx in memory locations I, I+1, and I+2
    /// </summary>
    private void OP_Fx33()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);
        byte value = Registers[x];

        // Ones-place (e.g., if value is 157, this gets 7)
        memory[I + 2] = (byte)(value % 10);
        value /= 10;

        // Tens-place (e.g., if value is 15, this gets 5)
        memory[I + 1] = (byte)(value % 10);
        value /= 10;

        // Hundreds-place (e.g., if value is 1, this gets 1)
        memory[I] = (byte)(value % 10);
    }

    /// <summary>
    ///     Store registers V0 through Vx in memory starting at location I
    /// </summary>
    private void OP_Fx55()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);

        for (byte i = 0; i <= x; ++i) memory[I + i] = Registers[i];
    }

    /// <summary>
    ///     Read registers V0 through Vx from memory starting at location I
    /// </summary>
    private void OP_Fx65()
    {
        byte x = (byte)((Opcode & 0x0F00) >> 8);

        for (byte i = 0; i <= x; ++i) Registers[i] = memory[I + i];
    }

    // Define the delegate type (Action is a built-in delegate for void functions with no params)
    private delegate void Chip8Instruction();
}