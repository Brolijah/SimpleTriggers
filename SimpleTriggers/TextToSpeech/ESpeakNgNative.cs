using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SimpleTriggers.TextToSpeech;

// https://github.com/espeak-ng/espeak-ng/blob/master/docs/integration.md
// espeak_ng.h - contains espeak_ng_** functions
//   https://github.com/espeak-ng/espeak-ng/blob/master/src/include/espeak-ng/espeak_ng.h
// speak_lib.h - contains espeak_** functions
//   https://github.com/espeak-ng/espeak-ng/blob/master/src/include/espeak-ng/speak_lib.h

public static partial class ESpeakNgNative {
    private static bool isResolverRegistered = false;
    private static nint pHandle;
    private static string libraryPath = "";
    public const string LibraryName = "espeak-ng-win-amd64.dll";

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L198
    // returns sample rate in Hz, or -1 (EE_INTERNAL_ERROR)
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int espeak_Initialize(EsAudioOutput output, int buflength, string path, EsPhoneme options);

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L220
    [LibraryImport(LibraryName)]
    public static partial void espeak_SetSynthCallback(EsSynthCallback? callback = null);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void EsSynthCallback(nint wav, int numsamples, nint esEvent); // short* wav, TODO : EVENT

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L294
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial EsError espeak_Synth(
        string text,
        uint position,
        EsPosition positionType,
        uint end_position,
        EsCharMode flags,
        nint unique_identifier,
        nint user_data
    );

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L437
    [LibraryImport(LibraryName)]
    public static partial EsError espeak_SetParameter(EsParameter param, int value, int relative);

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L520
    // First argument is const void** textptr
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial string espeak_TextToPhonemes(in nint textptr, EsCharMode textmode, int phonememode);

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L606
    // Return value is EsVoice**, size is unknown, must be found by locating the null terminator
    // Argument voicespec is a pointer to an existing EsVoice. Can be used to get an exclusive compatible list of voices for a lang
    [LibraryImport(LibraryName)]
    public static partial nint espeak_ListVoices(nint voicespec);

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L631
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial EsError espeak_SetVoiceByName(string name);

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L664
    // Return value is EsVoice*
    [LibraryImport(LibraryName)]
    public static partial nint espeak_GetCurrentVoice();

    // https://github.com/espeak-ng/espeak-ng/blob/fbe4b3764285c35b1f035cb8d09ad9fc19f71c30/src/include/espeak-ng/speak_lib.h#L672
    [LibraryImport(LibraryName)]
    public static partial EsError espeak_Cancel();

    [StructLayout(LayoutKind.Sequential)]
    public struct EsVoice
    {
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string name;         // a given name for this voice. UTF8 string.
        public nint languages;      // list of pairs of (byte) priority + (string) language (and dialect qualifier)
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string identifier;   // the filename for this voice within espeak-ng-data/voices
        public byte gender;         // 0=none 1=male, 2=female,
        public byte age;            // 0=not specified, or age in years
        public byte variant;        // only used when passed as a parameter to espeak_SetVoiceByProperties
        // Below are used internally by the library
        private byte xx1;
        private int score;
        private nint spare;
    };


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