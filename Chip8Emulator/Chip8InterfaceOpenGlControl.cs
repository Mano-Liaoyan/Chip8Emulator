using System;
using System.Diagnostics;
using Chip8Emulator.Core;
using OpenTK.Graphics.OpenGL;
using OpenTKAvalonia;

namespace Chip8Emulator;

public class Chip8InterfaceOpenGlControl : BaseTkOpenGlControl
{
    private readonly CPU cpu;
    private readonly int cycleDelay;
    private readonly Stopwatch stopwatch;

    private readonly float[] vertices =
    [
        // Positions    // TexCoords
        -1.0f, 1.0f, 0.0f, 0.0f, // Top Left
        -1.0f, -1.0f, 0.0f, 1.0f, // Bottom Left
        1.0f, -1.0f, 1.0f, 1.0f, // Bottom Right
        1.0f, 1.0f, 1.0f, 0.0f // Top Right
    ];

    private long lastCycleTime;
    private int shaderProgram;

    // OpenGL Objects
    private int textureHandle;
    private int vaoHandle;
    private int vboHandle;

    public Chip8InterfaceOpenGlControl()
    {
        Console.WriteLine("UI: Creating OpenGLControl");
        cpu = new CPU();
        cpu.LoadROM("opcode.ch8");
        cycleDelay = 1;
        stopwatch = Stopwatch.StartNew();
    }

    public void LoadRom(string path)
    {
        cpu.LoadROM(path);
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
    }

    protected override void OpenTkRender()
    {
        // 1. Emulation Timing Logic
        long currentTime = stopwatch.ElapsedMilliseconds;

        if (currentTime - lastCycleTime >= cycleDelay)
        {
            cpu.Cycle();
            lastCycleTime = currentTime;
        }


        // 2. Rendering
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.UseProgram(shaderProgram);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        // Upload the video buffer
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, cpu.video);

        GL.BindVertexArray(vaoHandle);
        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
    }

    protected override void OpenTkTeardown()
    {
        GL.DeleteTexture(textureHandle);
        GL.DeleteProgram(shaderProgram);
        GL.DeleteBuffer(vboHandle);
        GL.DeleteVertexArray(vaoHandle);
    }
}