using System;
using System.Reflection;
using System.Runtime.InteropServices;
using FFXIVClientStructs;

namespace SimpleTriggers.TextToSpeech;

// https://github.com/dectalk/dectalk/blob/32efa30ef2e216b3ad091c41abf5b502498a19aa/src/dapi/src/api/ttsapi.h
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
        uint deviceOptions,
        nint callbackRoutine,
        int instanceParameter,
        string dictionary
    );

    [Flags]
    public enum SpeechFlags : uint {
        Normal = 0,
        Force = 1
    }
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial uint TextToSpeechSpeak(nint handle, string text, SpeechFlags flags);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechSync(nint handle);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechSetSpeaker(nint handle, DecTalkVoice speaker);

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechOpenInMemory(nint handle, WaveFormat dwFormat);

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

    public enum StatusId : uint
    {
        INPUT_CHARACTER_COUNT = 0,
        STATUS_SPEAKING = 1,
        WAVE_OUT_DEVICE_ID = 2,
    }

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechGetStatus(
        nint handle, [In] StatusId[] identifiers, [Out] uint[] statuses, uint numStatuses);

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

    public enum WaveFormat : uint {
        WAVE_INVALIDFORMAT  =  0x00000000,       /* invalid format */
        WAVE_FORMAT_1M08    =  0x00000001,       /* 11.025 kHz, Mono,   8-bit */
        WAVE_FORMAT_1M16    =  0x00000004,       /* 11.025 kHz, Mono,   16-bit */
        WAVE_FORMAT_08M08   =  0x00001000,       /* 8      kHz, Mono,   8-bit */
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

        public nint MaximumBufferLength;
        public nint MaximumNumberOfPhonemeChanges;
        public nint MaximumNumberOfIndexMarks;

        public nint BufferLength;
        public nint NumberOfPhonemeChanges;
        public nint NumberOfIndexMarks;

        public nint Reserved;
    }
}