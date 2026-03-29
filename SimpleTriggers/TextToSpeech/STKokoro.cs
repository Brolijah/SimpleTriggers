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
    private readonly Task<IPA?> ipaTask;
    private readonly CancellationTokenSource cts = new();
    private float speed = 1.0f;
    private string lang = "en-us";
    private KokoroVoice kv;
    private KokoroPlayback kp;
    private KokoroJob? lastJob;
    public STKokoro(string binPath, string configPath)
    {
        this.configPath = configPath;
        ttsTask = LoadModelAsync();
        ipaTask = LoadDictionaryAsync(binPath);     
        Tokenizer.eSpeakNGPath = Path.Join(binPath, "espeak");
        KokoroVoiceManager.LoadVoicesFromPath(Path.Join(binPath,"voices"));
        kv = KokoroVoiceManager.GetVoice("af_bella");
        kp = new KokoroPlayback();
    }

    private async Task<IPA?> LoadDictionaryAsync(string path)
    {
        try
        {
            return new IPA(Path.Join(path, "en_US.txt"));
        } catch (Exception e)
        {
            STLog.Log.Error(e, "STKokoro.LoadDictionaryAsync(): Exception caught:");
            return null;
        }
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

    private bool TryGetIPA([NotNullWhen(true)] out IPA? ipa)
    {
        if(ipaTask.IsCompletedSuccessfully)
        {
            ipa = ipaTask.Result;
        } else { ipa = null; }

        return ipa != null;
    }

    private string GetModelPath()
    {
        return Path.Join(configPath, "kokoro-quant.onnx");
    }

    public void SetVoice(string strVoice)
    {
        kv = KokoroVoiceManager.GetVoice(strVoice);
    }

    // [0.0, 100.0]
    public void SetVolume(float volume)
    {
        var v = volume/100f; // scales it down between [0, 1]
        try
        {
            kp.SetVolume(v);
        } catch (Exception e)
        {
            STLog.Log.Warning(e,"Exception caught:");
        }
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

    public void SetLanguage(string lang)
    {
        this.lang = lang;
    }

    public void Speak(string message, bool extra)
    {
        if(TryGetKokoroTTS(out var tts) && TryGetIPA(out var ipa))
        {
            try
            {
                int[]? tokens;
                if(extra) tokens = Tokenizer.Tokenize(message, lang);
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
        return TryGetKokoroTTS(out _) && TryGetIPA(out _);
    }

    public void Dispose()
    {
        cts.Cancel();
        kp.Dispose();
        if(TryGetIPA(out var ipa))
        {
            ipa.Dispose();
        }
        if(TryGetKokoroTTS(out var tts))
        {
            tts.Dispose();
        }
    }
}
