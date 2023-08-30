
using eyecandy;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace multipass;

internal class Program
{
    static void Main(string[] args)
    {
        // uses eyecandy Shader class but not the base window
        ErrorLogging.Strategy = LoggingStrategy.AlwaysOutputToConsole;

        var otkWinSettings = GameWindowSettings.Default;
        var otkNativeSettings = NativeWindowSettings.Default;

        otkNativeSettings.Title = "OpenTK Multipass Demo";
        otkNativeSettings.Size = (960, 540);

        // Buh-bye, Raspberry Pi... too slow, too limited
        //otkNativeSettings.API = ContextAPI.OpenGLES;
        //otkNativeSettings.APIVersion = new Version(3, 2);
        //otkNativeSettings.Profile = ContextProfile.Core;

        // Windows & Linux, but not MacOS (ha!)
        //otkNativeSettings.APIVersion = new Version(4, 6);

        // Debug-message callbacks using the modern 4.3+ KHR style
        // https://opentk.net/learn/appendix_opengl/debug_callback.html?tabs=debug-context-4%2Cdelegate-gl%2Cenable-gl
        otkNativeSettings.Flags = ContextFlags.Debug;

        var win = new Win(otkWinSettings, otkNativeSettings);
        win.Focus();
        win.Run();
        win.Dispose();
    }
}
