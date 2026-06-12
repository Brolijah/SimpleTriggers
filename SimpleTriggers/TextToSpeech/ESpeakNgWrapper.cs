// This class will provide a simplified interface for espeak-ng's native calls
// Seeing as I intend to have both Kokoro and eSpeakTTS interface with this,
// it seems like the best solution to avoid writing duplicate code

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using SimpleTriggers.Logger;

namespace SimpleTriggers.TextToSpeech;

public static class ESpeakNgWrapper
{
    private static bool _initialized = false;

    // Must be called before anything else
    public static void Initialize(string binPath)
    {
        try {
            if(!_initialized)
            {
                var esPath = Path.Join(binPath, "espeak");
                var dllPath = Path.Join(esPath, ESpeakNgNative.LibraryName);

                STLog.Log.Warning($"espeak dll path = {dllPath}");
                ESpeakNgNative.SetupResolver(Path.Join(esPath, ESpeakNgNative.LibraryName));

                var res = ESpeakNgNative.espeak_Initialize(EsAudioOutput.AUDIO_OUTPUT_SYNCHRONOUS, 0, esPath, EsPhoneme.INIT_PHONEME_IPA);
                STLog.Log.Warning($"espeak_Initialize returned {res}");
                _initialized = true;
            }
        } catch (Exception e)
        {
            STLog.Log.Error(e, "ESpeakNgWrapper.Initialize(): Exception caught:");
            _initialized = false;
        }
    }

    public static bool IsInitialized()
    {
        return _initialized;
    }

    public static string ToPhonemes(string text)
    {
        if(!_initialized) return "";

        var builder = new StringBuilder();
        var clauses = Regex.Split(text, @"([\p{P}])"); // Split on punctuation
        foreach (var phrase in clauses)
        {
            var ptrPhrase = Marshal.StringToHGlobalAuto(phrase);
            string phonemes;
            try {
                phonemes = ESpeakNgNative.espeak_TextToPhonemes(in ptrPhrase, EsCharMode.espeakCHARS_AUTO, 1);
            } finally { Marshal.FreeHGlobal(ptrPhrase); }
            builder.Append(phonemes);
            builder.Append(' ');
        }
        return builder.ToString();
    }

    public static string[] GetVoices()
    {
        
        return [];
    }
}