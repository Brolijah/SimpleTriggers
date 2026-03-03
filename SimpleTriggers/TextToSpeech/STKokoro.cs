using System;
using System.IO;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using SimpleTriggers.Phonetics;
using Serilog;
using System.Threading.Tasks;
using System.Threading;

namespace SimpleTriggers.TextToSpeech;

public class STKokoro : ITextToSpeech, IDisposable
{
    private string assemblyPath {get;init;}
    private float volume = 1.0f;
    private float speed = 1.0f;
    private IPA ipa;
    private KokoroTTS tts;
    private KokoroVoice kv;
    private KokoroPlayback kp;
    public STKokoro(string path)
    {
        assemblyPath = path;
        ipa = new IPA(path, "en_US.txt");
        tts = KokoroTTS.LoadModel(Path.Join(assemblyPath, "kokoro-quant.onnx"));
        Tokenizer.eSpeakNGPath = Path.Join(assemblyPath, "espeak");
        KokoroVoiceManager.LoadVoicesFromPath(Path.Join(assemblyPath,"voices"));
        kv = KokoroVoiceManager.GetVoice("af_bella");
        kp = new KokoroPlayback();
        kp.Enqueue([]);
    }

    public void SetVoice(string strVoice)
    {
        kv = KokoroVoiceManager.GetVoice(strVoice);
    }

    public void SetVolume(float volume)
    {
        this.volume = volume/100.0f;
        try
        {
            kp.SetVolume(this.volume);
        } catch (Exception e)
        {
            Log.Error($"[Simple Triggers]: STKokoro.SetVolume(): Exception caught: {e.Message}");
        }
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

    public void Speak(string message)
    {
        try
        {
            var phonemes = ipa.EnglishToIPA(message);
            var tokens = Tokenizer.TokenizePhonemes(phonemes.ToCharArray());
            kp.StopPlayback();
            kp.SetVolume(volume);
            tts.EnqueueJob(KokoroJob.Create(tokens, kv, speed, kp.Enqueue));
        } catch (Exception e)
        {
            Log.Error($"[Simple Triggers]: STKokoro.Speak(): Exception caught: {e.Message}");
        }
    }

    public bool IsInitialized()
    {
        
        return true;
    }

    public void Dispose()
    {
        ipa.Dispose();
        kp.Dispose();
        tts.Dispose();
    }
}
