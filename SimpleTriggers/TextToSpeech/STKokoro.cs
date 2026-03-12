using System;
using System.IO;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using SimpleTriggers.Phonetics;
using Serilog;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Net.Http;

namespace SimpleTriggers.TextToSpeech;

public class STKokoro : ITextToSpeech, IDisposable
{
    // sha256 = c1610a859f3bdea01107e73e50100685af38fff88f5cd8e5c56df109ec880204
    private const string ModelUri = "https://github.com/taylorchu/kokoro-onnx/releases/download/v0.2.0/kokoro-quant.onnx";
    private readonly string configPath;
    private readonly Task<KokoroTTS> ttsTask;
    private float volume = 1.0f;
    private float speed = 1.0f;
    private IPA ipa;
    private KokoroVoice kv;
    private KokoroPlayback kp;
    private KokoroJob? lastJob;
    public STKokoro(string binPath, string configPath)
    {
        this.configPath = configPath;
        ipa = new IPA(Path.Join(binPath, "en_US.txt"));

        ttsTask = LoadModelAsync();
        Tokenizer.eSpeakNGPath = Path.Join(binPath, "espeak");
        KokoroVoiceManager.LoadVoicesFromPath(Path.Join(binPath,"voices"));
        kv = KokoroVoiceManager.GetVoice("af_bella");
        kp = new KokoroPlayback();
        kp.Enqueue([]);
    }

    private async Task<KokoroTTS> LoadModelAsync()
    {
        bool download = false;
        var path = GetModelPath();
        if(Path.Exists(path)) // if the model file exists on disk
        {
            var hash = SHA256.HashData(await File.ReadAllBytesAsync(path));
            if(!(Convert.ToHexStringLower(hash) == "c1610a859f3bdea01107e73e50100685af38fff88f5cd8e5c56df109ec880204"))
            {
                // mismatch, flag for download
                File.Delete(path);
                Log.Debug("[Simple Triggers]: KokoroTTS model mismatched hash, redownloading");
                download = true;
            } else { Log.Debug("[Simple Triggers]: KokoroTTS model already on disk. Using existing file."); }
        } else { download = true; }

        if(download)
        {
            Log.Debug("[Simple Triggers]: Downloading KokoroTTS model...");
            using var client = new HttpClient();
            using var response = await client.GetAsync(ModelUri, HttpCompletionOption.ResponseHeadersRead);
            using var responseStream = await response.Content.ReadAsStreamAsync();
            using(var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await responseStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
            Log.Debug("[Simple Triggers]: Kokoro model download completed");
        }

        return KokoroTTS.LoadModel(path);
    }

    private bool TryGetKokoroTTS([NotNullWhen(true)] out KokoroTTS? tts)
    {
        if(ttsTask.IsCompletedSuccessfully)
        {
            tts = ttsTask.Result;
            return true;
        }
        tts = null;
        return false;
    }

    private string GetModelPath()
    {
        return Path.Join(configPath, "kokoro-quant.onnx");
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

    public void Speak(string message, bool extra)
    {
        if(TryGetKokoroTTS(out var tts))
        {
            try
            {
                int[]? tokens;
                if(extra) tokens = Tokenizer.Tokenize(message);
                else      tokens = Tokenizer.TokenizePhonemes(ipa.EnglishToIPA(message).ToCharArray());

                lastJob?.Cancel();
                kp.StopPlayback();
                kp.SetVolume(volume);
                lastJob = tts.EnqueueJob(KokoroJob.Create(tokens, kv, speed, kp.Enqueue));
            } catch (Exception e)
            {
                Log.Error($"[Simple Triggers]: STKokoro.Speak(): Exception caught: {e.Message}");
            }
        } else {
            Log.Error("[Simple Triggers]: STKokoro.Speak(): Attempted TTS before model loaded.");
        }
    }

    public bool IsInitialized()
    {
        return TryGetKokoroTTS(out _);
    }

    public void Dispose()
    {
        ipa.Dispose();
        kp.Dispose();
        if(TryGetKokoroTTS(out var tts))
        {
            tts.Dispose();
        }
    }
}
