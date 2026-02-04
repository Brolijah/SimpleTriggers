using System;
using System.IO;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using SimpleTriggers.Phonetics;
using Serilog;

namespace SimpleTriggers.TextToSpeech;

public class STKokoro : IDisposable
{
    private string assemblyPath {get;init;}
    private IPA ipa {get;init;}
    private KokoroTTS tts {get;init;}
    private KokoroVoice kv {get;set;}
    public STKokoro(string path)
    {
        assemblyPath = path;
        ipa = new IPA(path, "en_US.txt");
        tts = KokoroTTS.LoadModel(Path.Join(assemblyPath, "kokoro-quant.onnx"));
        Tokenizer.eSpeakNGPath = Path.Join(assemblyPath, "espeak");
        KokoroVoiceManager.LoadVoicesFromPath(Path.Join(assemblyPath,"voices"));
        kv = KokoroVoiceManager.GetVoice("af_bella");
    }

    public void Speak(string message)
    {
        try
        {
            var phonemes = ipa.EnglishToIPA(message);
            var tokens = Tokenizer.TokenizePhonemes(phonemes.ToCharArray());
            tts.Speak_Phonemes(message, tokens, kv, default, true);
            //tts.StopPlayback();
            //tts.SpeakFast(message, kv);
        } catch (Exception e)
        {
            Log.Error($"[Simple Triggers]: Exception caught: {e.Message}");
        }
        
    }

    public void SetVoice(string strVoice)
    {
        kv = KokoroVoiceManager.GetVoice(strVoice);
    }

    public void Dispose()
    {
        tts.Dispose();
    }
}
