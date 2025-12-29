using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;
using OpenTK.Graphics.OpenGL;

namespace OpenTKAvalonia;

public abstract class BaseTkOpenGlControl : OpenGlControlBase, ICustomHitTest
{
    /// <summary>
    ///     KeyboardState provides an easy-to-use, stateful wrapper around Avalonia's Keyboard events, as OpenTK keyboard
    ///     states are not handled.
    ///     You can access full keyboard state for both the current frame and the previous one through this object.
    /// </summary>
    protected readonly AvaloniaKeyboardState KeyboardState = new();

    private AvaloniaTkContext? _avaloniaTkContext;

    private double? _renderScaling;

    private double RenderScaling => (_renderScaling ??= TopLevel.GetTopLevel(this)?.RenderScaling)
                                    ?? throw new PlatformNotSupportedException("Could not obtain TopLevel");

    public bool HitTest(Point point)
    {
        return Bounds.Contains(point);
    }

    /// <summary>
    ///     OpenTkRender is called once a frame to draw to the control.
    ///     You can do anything you want here, but make sure you undo any configuration changes after, or you may get weirdness
    ///     with other controls.
    /// </summary>
    protected virtual void OpenTkRender()
    {
    }

    /// <summary>
    ///     OpenTkInit is called once when the control is first created.
    ///     At this point, the GL bindings are initialized, and you can invoke GL functions.
    ///     You could use this function to load and compile shaders, load textures, allocate buffers, etc.
    /// </summary>
    protected virtual void OpenTkInit()
    {
    }

    /// <summary>
    ///     OpenTkTeardown is called once when the control is destroyed.
    ///     Though GL bindings are still valid, as OpenTK provides no way to clear them, you should not invoke GL functions
    ///     after this function finishes executing.
    ///     At best, they will do nothing, at worst, something could go wrong.
    ///     You should use this function as a last chance to clean up any GL resources you have allocated - delete buffers,
    ///     vertex arrays, programs, and textures.
    /// </summary>
    protected virtual void OpenTkTeardown()
    {
    }

    protected sealed override void OnOpenGlRender(GlInterface gl, int fb)
    {
        //Update last key states
        KeyboardState.OnFrame();

        //Set up the aspect ratio so shapes aren't stretched.
        //GL.Viewport(0, 0, (int) Bounds.Width, (int) Bounds.Height);

        var (w, h) = GetPlatformSpecificBounds();
        GL.Viewport(0, 0, w, h);

        //Tell our subclass to render
        if (Bounds.Width != 0 && Bounds.Height != 0)
        {
            OpenTkRender();
        }
        else
        {
            Console.WriteLine($"[BaseTk] OnOpenGlRender skipped: Bounds is zero ({Bounds.Width}x{Bounds.Height})");
        }

        //Schedule next UI update with avalonia
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Background);
    }

    private (int width, int height) GetPlatformSpecificBounds()
    {
        return (Math.Max(1, (int)(Bounds.Width * RenderScaling)), Math.Max(1, (int)(Bounds.Height * RenderScaling)));
    }


    protected sealed override void OnOpenGlInit(GlInterface gl)
    {
        Console.WriteLine("[BaseTk] OnOpenGlInit started");
        try
        {
            //Initialize the OpenTK<->Avalonia Bridge
            _avaloniaTkContext = new AvaloniaTkContext(gl);
            GL.LoadBindings(_avaloniaTkContext);

            //Invoke the subclass' init function
            OpenTkInit();
            Console.WriteLine("[BaseTk] OnOpenGlInit finished successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BaseTk] OnOpenGlInit CRITICAL ERROR: {ex}");
            throw;
        }
    }

    //Simply call the subclass' teardown function
    protected sealed override void OnOpenGlDeinit(GlInterface gl)
    {
        Console.WriteLine("[BaseTk] OnOpenGlDeinit called");
        OpenTkTeardown();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEffectivelyVisible)
            return;

        KeyboardState.SetKey(e.Key, true);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (!IsEffectivelyVisible)
            return;

        KeyboardState.SetKey(e.Key, false);
    }
}