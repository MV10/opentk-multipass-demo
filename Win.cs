
using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace multipass;

internal partial class Win : GameWindow, IDisposable
{
    static DebugProcKhr DebugMessageDelegate = OnDebugMessage;

    Color4 backgroundColor = new(0, 0, 0, 1);
    FrameData frameData = new();
    List<Shader> shaders = new();
    List<(int hBuffer, int hTexture)> framebuffer = new();
    Stopwatch Clock = new();

    public Win(GameWindowSettings gameWindow, NativeWindowSettings nativeWindow)
        : base(gameWindow, nativeWindow)
    {
        GL.Khr.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);

        Clock.Start();
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(backgroundColor);

        // Three buffers:
        // pass1-plasma:     draw 0
        // pass2-desaturate: draw 1, sample 0 - plasma 0 as input
        // pass3-sobel:      draw 2, sample 1 - edge detection with desaturate 1 as input
        // pass4-clouds:     draw 1, sample 2 - apply cloudy edges to desaturated 2 input
        // pass5-colorize:   draw 2, sample 0,1 - colorize clouds and sobel lines from 1 using plasma colors from 0
        // blitter output 2
        InitializeFrameBuffers(3);

        // Shaders are applied in the order loaded here
        shaders.Add(new("passthrough.vert", "pass1-plasma.frag"));
        shaders.Add(new("passthrough.vert", "pass2-desaturate.frag"));
        shaders.Add(new("passthrough.vert", "pass3-sobel.frag"));
        shaders.Add(new("passthrough.vert", "pass4-clouds.frag"));
        shaders.Add(new("passthrough.vert", "pass5-colorize.frag"));

        // later instead of sequential, multi-buffer mixing
        // https://www.shadertoy.com/view/flsBD8
        // maybe conf defines # of buffers and inputs/outputs

        // These all use the same vertex shader, so we only need to
        // provide one example -- see FrameData comments on the
        // VertexArrayObject field for more details.
        frameData.InitializeVertexData(shaders[0]);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        var res = new Vector2(ClientSize.X, ClientSize.Y);
        var time = (float)Clock.Elapsed.TotalSeconds;

        // pass1-plasma
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, framebuffer[0].hBuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        shaders[0].SetUniform("resolution", res);
        shaders[0].SetUniform("time", time);
        frameData.Draw();

        // pass2-desaturate
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, framebuffer[1].hBuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        shaders[1].SetUniform("resolution", res);
        shaders[1].SetTexture("input0", framebuffer[0].hTexture, TextureUnit.Texture0);
        frameData.Draw();

        // pass3-sobel
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, framebuffer[2].hBuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        shaders[2].SetUniform("resolution", res);
        shaders[2].SetTexture("input1", framebuffer[1].hTexture, TextureUnit.Texture0);
        frameData.Draw();

        // pass4-clouds
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, framebuffer[1].hBuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        shaders[3].SetUniform("resolution", res);
        shaders[3].SetUniform("time", time);
        shaders[3].SetTexture("input2", framebuffer[2].hTexture, TextureUnit.Texture0);
        frameData.Draw();

        // pass5-colorize
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, framebuffer[2].hBuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        shaders[4].SetUniform("resolution", res);
        shaders[4].SetTexture("input0", framebuffer[0].hTexture, TextureUnit.Texture0);
        shaders[4].SetTexture("input1", framebuffer[1].hTexture, TextureUnit.Texture1);
        frameData.Draw();

        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer[2].hBuffer);
        GL.BlitFramebuffer(
            0, 0, ClientSize.X, ClientSize.Y,
            0, 0, ClientSize.X, ClientSize.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);

        var bufferCount = framebuffer.Count;
        framebuffer.Clear();
        InitializeFrameBuffers(bufferCount);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        var input = KeyboardState;

        if (input.IsKeyReleased(Keys.Escape))
        {
            Close();
            return;
        }
    }

    protected new void Dispose()
    {
        base.Dispose();
        // TODO release or dispose framebuffers and/or textures?
        foreach (var s in shaders) s.Dispose();
        shaders.Clear();
    }

    void InitializeFrameBuffers(int count)
    {
        for(int i = 0; i < count; i++)
        {
            var framebufferHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferHandle);

            var textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, ClientSize.X, ClientSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureHandle, 0);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if(!status.Equals(FramebufferErrorCode.FramebufferComplete) && !status.Equals(FramebufferErrorCode.FramebufferCompleteExt))
            {
                Console.WriteLine($"Error creating framebuffer {i}: {status}");
                Thread.Sleep(250);
                Environment.Exit(-1);
            }

            framebuffer.Add((framebufferHandle, textureHandle));
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private static void OnDebugMessage(
        DebugSource source,     // Source of the debugging message.
        DebugType type,         // Type of the debugging message.
        int id,                 // ID associated with the message.
        DebugSeverity severity, // Severity of the message.
        int length,             // Length of the string in pMessage.
        IntPtr pMessage,        // Pointer to message string.
        IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
    {
        // ignore the noise
        if (id == 131185) return;

        // In order to access the string pointed to by pMessage, you can use Marshal
        // class to copy its contents to a C# string without unsafe code. You can
        // also use the new function Marshal.PtrToStringUTF8 since .NET Core 1.1.
        string message = Marshal.PtrToStringAnsi(pMessage, length);

        // The rest of the function is up to you to implement, however a debug output
        // is always useful.
        Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);

        // Potentially, you may want to throw from the function for certain severity
        // messages.
        //if (type == DebugType.DebugTypeError)
        //{
        //    throw new Exception(message);
        //}
    }
}
