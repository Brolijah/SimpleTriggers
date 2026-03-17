using System;
using System.IO;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using SimpleTriggers.Phonetics;
using SimpleTriggers.Logger;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Net.Http;
using System.Threading;

namespace SimpleTriggers.TextToSpeech;

public class STKokoro : ITextToSpeech
{
    // sha256 = c1610a859f3bdea01107e73e50100685af38fff88f5cd8e5c56df109ec880204
    private const string ModelUri = "https://github.com/taylorchu/kokoro-onnx/releases/download/v0.2.0/kokoro-quant.onnx";
    private readonly string configPath;
    private readonly Task<KokoroTTS?> ttsTask;
    private readonly CancellationTokenSource cts = new();
    private float speed = 1.0f;
    private IPA ipa;
    private KokoroVoice kv;
    //private KokoroPlayback kp;
    private AudioPlayer kp;
    private KokoroJob? lastJob;
    public STKokoro(string binPath, string configPath)
    {
        this.configPath = configPath;
        ipa = new IPA(Path.Join(binPath, "en_US.txt"));

        ttsTask = LoadModelAsync();
        Tokenizer.eSpeakNGPath = Path.Join(binPath, "espeak");
        KokoroVoiceManager.LoadVoicesFromPath(Path.Join(binPath,"voices"));
        kv = KokoroVoiceManager.GetVoice("af_bella");
        //kp = new KokoroPlayback();
        kp = new AudioPlayer();
        kp.Enqueue([]);
    }

    private async Task<KokoroTTS?> LoadModelAsync()
    {
        bool download = false;
        var path = GetModelPath();
        try {
            if(Path.Exists(path)) // if the model file exists on disk
            {
                var hash = SHA256.HashData(await File.ReadAllBytesAsync(path, cts.Token));
                if(!(Convert.ToHexStringLower(hash) == "c1610a859f3bdea01107e73e50100685af38fff88f5cd8e5c56df109ec880204"))
                {
                    // mismatch, flag for download
                    File.Delete(path);
                    STLog.Log.Information("KokoroTTS model mismatched hash, redownloading");
                    download = true;
                } else { STLog.Log.Information("KokoroTTS model already on disk. Using existing file."); }
            } else { download = true; }

            if(download)
            {
                STLog.Log.Information("Downloading KokoroTTS model...");
                using var client = new HttpClient();
                using var response = await client.GetAsync(ModelUri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                using var responseStream = await response.Content.ReadAsStreamAsync(cts.Token);
                using(var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await responseStream.CopyToAsync(fileStream, cts.Token);
                    await fileStream.FlushAsync(cts.Token);
                }
                STLog.Log.Information("Kokoro model download completed");
            }
        } catch (Exception e)
        {
            STLog.Log.Error(e, "STKokoro.LoadModelAsync(): Exception caught:");
            return null;
        }

        return KokoroTTS.LoadModel(path);
    }

    private bool TryGetKokoroTTS([NotNullWhen(true)] out KokoroTTS? tts)
    {
        if(ttsTask.IsCompletedSuccessfully)
        {
            tts = ttsTask.Result;
        } else { tts = null; }

        return tts != null;
    }

    private string GetModelPath()
    {
        return Path.Join(configPath, "kokoro-quant.onnx");
    }

    public void SetVoice(string strVoice)
    {
        kv = KokoroVoiceManager.GetVoice(strVoice);
    }

    // [0.0, 100.0], though technically would allow values over 100
    public void SetVolume(float volume)
    {
        try
        {
            kp.SetVolume(volume/100f);
        } catch (Exception e)
        {
            STLog.Log.Warning(e,"Exception caught:");
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
                if(extra) tokens = Tokenizer.Tokenize(message, "en-us");
                else      tokens = Tokenizer.TokenizePhonemes(ipa.EnglishToIPA(message).ToCharArray());

                lastJob?.Cancel();
                kp.StopPlayback();
                lastJob = tts.EnqueueJob(KokoroJob.Create(tokens, kv, speed, kp.Enqueue));
            } catch (Exception e)
            {
                STLog.Log.Error(e, "STKokoro.Speak(): Exception caught: ");
            }
        } else {
            STLog.Log.Warning("Attempted TTS before model loaded.");
        }
    }

    public bool IsInitialized()
    {
        return TryGetKokoroTTS(out _);
    }

    public void Dispose()
    {
        cts.Cancel();
        ipa.Dispose();
        kp.Dispose();
        if(TryGetKokoroTTS(out var tts))
        {
            tts.Dispose();
        }
    }
}
