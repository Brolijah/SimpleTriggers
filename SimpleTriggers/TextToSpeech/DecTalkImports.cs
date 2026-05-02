using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SimpleTriggers.TextToSpeech;

// https://github.com/dectalk/dectalk/blob/32efa30ef2e216b3ad091c41abf5b502498a19aa/src/dapi/src/api/ttsapi.h
public unsafe partial class DecTalkImports {
    public const int WaveMapper = -1;
    public const int StatusSpeaking = 1;

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

    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechGetStatus(
        nint handle, [In] uint[] identifiers, [Out] uint[] statuses, uint numStatuses
    );

    // yes, `reset` is a four byte bool here https://dectalk.github.io/dectalk/idh_sdk_2_texttospeechreset.htm
    [LibraryImport(LibraryName)]
    public static partial uint TextToSpeechReset(nint handle, [MarshalAs(UnmanagedType.Bool)] bool reset);

    public static void SetupResolver(string dllPath) =>
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(),
            (name, assembly, path) => name == LibraryName
                ? NativeLibrary.Load(dllPath)
                : NativeLibrary.Load(name, assembly, path));

    [Flags]
    public enum SpeechFlags : uint {
        Normal = 0,
        Force = 1
    }

    public enum WaveFormat : uint {
        WAVE_INVALIDFORMAT  =  0x00000000,       /* invalid format */
        WAVE_FORMAT_1M08    =  0x00000001,       /* 11.025 kHz, Mono,   8-bit */
        WAVE_FORMAT_1M16    =  0x00000004,       /* 11.025 kHz, Mono,   16-bit */
        WAVE_FORMAT_08M08   =  0x00001000,       /* 8      kHz, Mono,   8-bit */
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

        public nint MaximumBufferLength;
        public nint MaximumNumberOfPhonemeChanges;
        public nint MaximumNumberOfIndexMarks;

        public nint BufferLength;
        public nint NumberOfPhonemeChanges;
        public nint NumberOfIndexMarks;

        public nint Reserved;
    }
}