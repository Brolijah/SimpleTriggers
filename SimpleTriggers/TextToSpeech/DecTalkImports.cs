using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SimpleTriggers.TextToSpeech;

#if DEBUG
// https://github.com/dectalk/DECtalkMini/blob/dectalk-develop/include/epsonapi.h
public partial class DecTalkImports {
    private static bool isResolverRegistered = false;
    private static nint pHandle;
    private static string libraryPath = "";
    // Placeholder for the import resolver (see below)
    private const string LibraryName = "dtc";

    [LibraryImport(LibraryName)]
    public static partial int TextToSpeechInit(
        CallbackDelegate? callback = null,
        nint dictionary = 0
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate nint CallbackDelegate(nint buffer, long length, int phoneme);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int TextToSpeechStart(string text, nint unused, DtWaveFormat format);

    [LibraryImport(LibraryName)]
    public static partial int TextToSpeechReset();

    [LibraryImport(LibraryName)]
    public static partial int TextToSpeechSync();

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int TextToSpeechChangeVoice(string voice);

    [LibraryImport(LibraryName)]
    public static partial void TextToSpeechSetRate(int rate);


    public static void SetupResolver(string dllPath)
    {
        if(!isResolverRegistered)
        {
            isResolverRegistered = true;
            libraryPath = dllPath;
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), ImportResolver);
        }
    }
    private static IntPtr ImportResolver(string library, Assembly assembly, DllImportSearchPath? path)
    {
        if(library == LibraryName)
        {
            if(pHandle == IntPtr.Zero)
            {
                pHandle = NativeLibrary.Load(libraryPath, assembly, path);
            }
            return pHandle;
        }
        return IntPtr.Zero;
    }
}
#endif
