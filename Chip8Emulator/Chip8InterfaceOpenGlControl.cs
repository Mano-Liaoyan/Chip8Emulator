using System;
using System.Diagnostics;
using Avalonia.Input;
using Chip8Emulator.Core;
using OpenTK.Graphics.OpenGL;
using OpenTKAvalonia;

namespace Chip8Emulator;

public class Chip8InterfaceOpenGlControl : BaseTkOpenGlControl
{
    private const double CpuFrequency = 700.0;
    private const double TimerFrequency = 60.0;
    private const double CpuPeriod = 2000.0 / CpuFrequency;
    private const double TimerPeriod = 1000.0 / TimerFrequency;
    private readonly Stopwatch stopwatch;

    private readonly float[] vertices =
    [
        // Positions    // TexCoords
        -1.0f, 1.0f, 0.0f, 0.0f, // Top Left
        -1.0f, -1.0f, 0.0f, 1.0f, // Bottom Left
        1.0f, -1.0f, 1.0f, 1.0f, // Bottom Right
        1.0f, 1.0f, 1.0f, 0.0f // Top Right
    ];

    private double _cpuAccumulator;

    private SoundPlayer? _soundPlayer;
    private double _timerAccumulator;
    private bool IsRomLoaded;

    private long lastCycleTime;
    private int shaderProgram;

    // OpenGL Objects
    private int textureHandle;
    private int vaoHandle;
    private int vboHandle;

    public Chip8InterfaceOpenGlControl()
    {
        Console.WriteLine("UI: Creating OpenGLControl");
        Cpu = new CPU();
        // Cpu.LoadROM("opcode.ch8");
        stopwatch = Stopwatch.StartNew();
    }

    public CPU Cpu { get; }

    public void LoadRom(string path)
    {
        Cpu.LoadROM(path);
        IsRomLoaded = true;
    }

    protected override void OpenTkInit()
    {
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

        // 1. Shaders
        const string vertexShaderSource = """
                                                      #version 330 core
                                                      layout (location = 0) in vec2 aPos;
                                                      layout (location = 1) in vec2 aTexCoord;
                                                      out vec2 TexCoord;
                                                      void main() {
                                                          gl_Position = vec4(aPos, 0.0, 1.0);
                                                          TexCoord = aTexCoord;
                                                      }
                                          """;

        const string fragmentShaderSource = """
                                                        #version 330 core
                                                        out vec4 FragColor;
                                                        in vec2 TexCoord;
                                                        uniform sampler2D texture1;
                                                        void main() {
                                                            FragColor = texture(texture1, TexCoord);
                                                        }
                                            """;

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);

        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);

        GL.DetachShader(shaderProgram, vertexShader);
        GL.DetachShader(shaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        // 2. Vertex Data
        vaoHandle = GL.GenVertexArray();
        vboHandle = GL.GenBuffer();

        GL.BindVertexArray(vaoHandle);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // 3. Texture
        textureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        try
        {
            _soundPlayer = new SoundPlayer();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Audio initialization failed: {e.Message}");
        }
    }

    protected override void OpenTkRender()
    {
        if (!IsRomLoaded) return;

        // 1. Emulation Timing Logic
        ProcessInput();
        long currentTime = stopwatch.ElapsedMilliseconds;
        double deltaTime = currentTime - lastCycleTime;
        lastCycleTime = currentTime;

        // Cap deltaTime to avoid spiral of death
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

        if (Cpu.SoundTimer > 0)
            _soundPlayer?.Play();
        else
            _soundPlayer?.Stop();


        // 2. Rendering
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.UseProgram(shaderProgram);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        // Upload the video buffer
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Cpu.video);

        GL.BindVertexArray(vaoHandle);
        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
    }

    protected override void OpenTkTeardown()
    {
        GL.DeleteTexture(textureHandle);
        GL.DeleteProgram(shaderProgram);
        GL.DeleteBuffer(vboHandle);
        GL.DeleteVertexArray(vaoHandle);
        _soundPlayer?.Dispose();
    }

    private void ProcessInput()
    {
        Cpu.SetKey(0x1, KeyboardState.IsKeyDown(Key.D1));
        Cpu.SetKey(0x2, KeyboardState.IsKeyDown(Key.D2));
        Cpu.SetKey(0x3, KeyboardState.IsKeyDown(Key.D3));
        Cpu.SetKey(0xC, KeyboardState.IsKeyDown(Key.D4));

        Cpu.SetKey(0x4, KeyboardState.IsKeyDown(Key.Q));
        Cpu.SetKey(0x5, KeyboardState.IsKeyDown(Key.W));
        Cpu.SetKey(0x6, KeyboardState.IsKeyDown(Key.E));
        Cpu.SetKey(0xD, KeyboardState.IsKeyDown(Key.R));

        Cpu.SetKey(0x7, KeyboardState.IsKeyDown(Key.A));
        Cpu.SetKey(0x8, KeyboardState.IsKeyDown(Key.S));
        Cpu.SetKey(0x9, KeyboardState.IsKeyDown(Key.D));
        Cpu.SetKey(0xE, KeyboardState.IsKeyDown(Key.F));

        Cpu.SetKey(0xA, KeyboardState.IsKeyDown(Key.Z));
        Cpu.SetKey(0x0, KeyboardState.IsKeyDown(Key.X));
        Cpu.SetKey(0xB, KeyboardState.IsKeyDown(Key.C));
        Cpu.SetKey(0xF, KeyboardState.IsKeyDown(Key.V));
    }
}