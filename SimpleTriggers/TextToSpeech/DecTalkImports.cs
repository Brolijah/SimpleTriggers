using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SimpleTriggers.TextToSpeech;

#if DEBUG
// https://github.com/dectalk/dectalk/blob/develop/src/dapi/src/api/ttsapi.h
public unsafe partial class DecTalkImports {
    private static bool isResolverRegistered = false;
    private static nint pHandle;
    private static string libraryPath = "";
    // Placeholder for the import resolver (see below)
    private const string LibraryName = "dtalk_us";

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial uint TextToSpeechStartupExFonix(
        ref nint handle,
        int deviceNumber,
        DtDeviceOptions deviceOptions,
        CallbackDelegate? callbackRoutine,
        int instanceParameter,
        string dictionary
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CallbackDelegate(long param1, long param2, uint cbParameter, uint uiMsg);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial uint TextToSpeechSpeak(nint handle, string text, DtSpeechFlags flags);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechSync(nint handle);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechSetSpeaker(nint handle, DecTalkVoice speaker);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechOpenInMemory(nint handle, DtWaveFormat dwFormat);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechCloseInMemory(nint handle);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechAddBuffer(nint handle, TTS_BUFFER* ttsBuffer);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechReturnBuffer(nint handle, TTS_BUFFER* ttsBuffer);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechSetRate(nint handle, uint rate);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechShutdown(nint handle);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechGetStatus(
        nint handle, [In] DtStatusId[] identifiers, [Out] uint[] statuses, uint numStatuses);

    // yes, `reset` is a four byte bool here https://dectalk.github.io/dectalk/idh_sdk_2_texttospeechreset.htm
    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechReset(nint handle, [MarshalAs(UnmanagedType.Bool)] bool reset);

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

    public static void Free()
    {
        NativeLibrary.Free(pHandle);
        pHandle = IntPtr.Zero;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
    public static bool IsLoaded(string dllPath)
    {
        return GetModuleHandle(dllPath) != IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TTS_PHONEME {
        public nint Phoneme;
        public nint PhonemeSampleNumber;
        public nint PhonemeDuration;
        public readonly nint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TTS_INDEX
    {
        public nint IndexValue;
        public nint IndexSampleNumber;
        public readonly nint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TTS_BUFFER
    {
        public IntPtr Data;
        public TTS_PHONEME* PhonemeArray;
        public TTS_INDEX* IndexArray;

        public uint MaximumBufferLength;
        public uint MaximumNumberOfPhonemeChanges;
        public uint MaximumNumberOfIndexMarks;

        public uint BufferLength;
        public uint NumberOfPhonemeChanges;
        public uint NumberOfIndexMarks;

        public uint Reserved;
    }
}
#endif
