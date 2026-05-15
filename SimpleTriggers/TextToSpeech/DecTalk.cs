using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SimpleTriggers.Logger;

namespace SimpleTriggers.TextToSpeech;

public class DecTalk : ITextToSpeech {
    private bool _initialized = false;
    private DecTalkImports.CallbackDelegate cb;
    private readonly Task<bool> libraryTask;
    private readonly AudioPlayer audioPlayer;
    private readonly CancellationTokenSource cts = new();
    private readonly string configPath;
    private const string ZipUrl = "https://github.com/Brolijah/DECtalkMini/releases/download/latest/speak-windows.zip";
    private readonly Lock speakLock = new();
    private byte[] activeBuffer = [];
    private string voice = "np"; // Paul
    private int speed = 200;

    public DecTalk(string configPath, AudioPlayer player)
    {
        cb = Callback;
        audioPlayer = player;
        audioPlayer.SetSourceWaveFormat(11025, 1);
        libraryTask = LoadLibraryAsync(this.configPath = configPath);
    }

    private async Task<bool> LoadLibraryAsync(string configPath)
    {
        // https://github.com/Brolijah/DECtalkMini/releases/download/latest/speak-windows.zip
        //   sha256: dad36a0f85ce7ad122837ac8060ec22a897c872e3435d6ede711211d766192ff
        // speak-win64-nofs/dtc.dll
        //   sha256: 00f9cf75201d503bbcc5af2828e0796abc45b584e70d50f95efbdf5957b36e34
        
        bool download = false;
        var zipFiles = new[] {
            (path: "speak-win64-nofs/dtc.dll", hash: "00f9cf75201d503bbcc5af2828e0796abc45b584e70d50f95efbdf5957b36e34"),
        };

        var resPath = Path.Join(configPath, "dectalk");
        try {
            foreach(var file in zipFiles)
            {
                var fileOnDisk = Path.Join(resPath, Path.GetFileName(file.path));
                if(Path.Exists(fileOnDisk))
                {
                    var hash = SHA256.HashData(await File.ReadAllBytesAsync(fileOnDisk, cts.Token));
                    if(!(Convert.ToHexStringLower(hash) == file.hash))
                    {
                        // mismatch, flag for download
                        File.Delete(fileOnDisk);
                        STLog.Log.Warning($"DECtalk mismatched hash for: {Path.GetFileName(file.path)}");
                        download = true;
                    } else { STLog.Log.Information($"DECtalk valid file on disk: {Path.GetFileName(file.path)}"); }
                } else { download = true; }
            }

            if(download)
            {
                STLog.Log.Information("Downloading DECtalk files...");
                if(!Directory.Exists(resPath)) Directory.CreateDirectory(resPath);
                using var client = new HttpClient();
                var zipData = await client.GetByteArrayAsync(ZipUrl);
                if(!(Convert.ToHexStringLower(SHA256.HashData(zipData)) == "dad36a0f85ce7ad122837ac8060ec22a897c872e3435d6ede711211d766192ff"))
                {
                    STLog.Log.Error("Something is wrong with the source DECtalk archive! Aborting download!!");
                    return false;
                }
                using var stream = new MemoryStream(zipData);
                using var zip = new ZipArchive(stream);
                foreach(var file in zipFiles)
                {
                    var entry = zip.GetEntry(file.path)!;
                    var outputPath = Path.Combine(resPath, Path.GetFileName(file.path));

                    await using var bytes = entry.Open();
                    await using var output = File.OpenWrite(outputPath);
                    await bytes.CopyToAsync(output);
                    await output.FlushAsync();
                }
            }
        } catch (Exception e) {
            STLog.Log.Error(e, "DecTalk.LoadLibraryAsync(): Exception caught:");
            return false;
        }
        return true;
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
        libraryTask.Dispose();
    }

    public void Speak(string text, bool extra)
    {
        if(!IsInitialized()) return;
        lock(speakLock)
        {
            try {
                activeBuffer = [];
                SetVoice(this.voice);
                SpeakInternal(text);
                Sync();
                audioPlayer.Enqueue(TrimAudioBuffer(activeBuffer));
            } catch (Exception e)
            {
                STLog.Log.Error(e, "Exception caught:");
            } finally { activeBuffer = []; }
        }
    }

    private static byte[] TrimAudioBuffer(byte[] buffer, float threshold = 0.01f)
    {
        // 16-bit PCM = 2 bytes per sample
        var sampleCount = buffer.Length / 2;
        var samples = new float[sampleCount];
        
        // converts the byte stream to float samples
        for (var n = 0; n < sampleCount; n++)
        {
            // Combine two bytes into a 16-bit signed integer
            short sample = BitConverter.ToInt16(buffer, n * 2);
            // Normalize to a float between -1.0 and 1.0
            samples[n] = sample / 32768f;
        }
        // Find the start above the threshold
        var start = 0;
        for (var i = 0; i < samples.Length; ++i)
        {
            if(Math.Abs(samples[i]) > threshold)
            {
                start = i;
                break;
            }
        }

        // Find the last sample above threshold
        var end = samples.Length - 1;
        for (var i = samples.Length - 1; i > 0; i--)
        {
            if (Math.Abs(samples[i]) > threshold)
            {
                end = i;
                break;
            }
        }

        var trimmedCount = end - (start+1);
        if(trimmedCount <= 0) return []; // entire stream silent

        // Extract the non-silent range back into a byte array
        var trimmedBytes = new byte[trimmedCount * 2];
        Buffer.BlockCopy(buffer, start * 2, trimmedBytes, 0, trimmedBytes.Length);
        
        return trimmedBytes;
    }

    public void SetVoice(string voice)
    {
        this.voice = voice;
        if(!IsInitialized()) return;
        try {
            AssertCall(DecTalkImports.TextToSpeechChangeVoice(voice), "ChangeVoice");
        } catch (Exception e) {
            STLog.Log.Error(e, "Exception caught:");
        }
    }
    
    public void SetVolume(float volume)
    {
        audioPlayer.SetVolume(volume);
    }

    public void SetSpeed(float speed)
    {
        this.speed = (int)speed;
        if(!IsInitialized()) return;
        DecTalkImports.TextToSpeechSetRate(this.speed);
    }

    public void SetLanguage(string lang)
    { } // Unused, DecTalk only supports English

    public bool IsInitialized()
    {
        bool ret;
        if(libraryTask.IsCompletedSuccessfully)
        {
            ret = libraryTask.Result && TryInitLibrary();
        } else { ret = false; }
        return ret;
    }

    private void Reset()
    {
        AssertCall(DecTalkImports.TextToSpeechReset(), "Reset");
    }

    private void SpeakInternal(string text)
    {
        AssertCall(DecTalkImports.TextToSpeechStart(text, 0, DtWaveFormat.WAVE_FORMAT_1M16), "Start");
    }

    private void Sync()
    {
        AssertCall(DecTalkImports.TextToSpeechSync(), "Sync");
    }

    private static void AssertCall(int value, string method) {
        if (value != 0) throw new Exception($"TextToSpeech{method} returned error code {(DtError)value}");
    }

    private bool TryInitLibrary()
    {
        if(!_initialized)
        {
            try {
                DecTalkImports.SetupResolver(Path.Join(configPath, "dectalk/dtc.dll"));
                AssertCall(DecTalkImports.TextToSpeechInit(cb), "Init");
                // workaround for a race condition where these may be set before the library is loaded
                DecTalkImports.TextToSpeechChangeVoice(this.voice); // not respected, despite calling this here, still uses Paul
                DecTalkImports.TextToSpeechSetRate(this.speed); // respected
                _initialized = true;
            } catch (Exception e) {
                STLog.Log.Error(e, "DecTalk.TryInitLibrary(): Exception caught:");
                _initialized = false;
            }
        }
        return _initialized;
    }

    // buffer is a short*
    // so, your byte[] will be length*2
    private nint Callback(nint buffer, long length, int phoneme)
    {
        try {
            unsafe {
                var data = new byte[activeBuffer.Length + (length * 2)];
                Buffer.BlockCopy(activeBuffer, 0, data, 0, activeBuffer.Length);
                Marshal.Copy(buffer, data, activeBuffer.Length, (int)(length*2));
                activeBuffer = data;
            }
        } catch (Exception e) {
            STLog.Log.Error(e, "Exception caught");
        }
        return 0;
    }
}
