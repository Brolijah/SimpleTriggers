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
    private nint handle = IntPtr.Zero;
    private readonly Task<bool> libraryTask;
    private readonly AudioPlayer audioPlayer;
    private readonly CancellationTokenSource cts = new();
    private readonly string configPath;
    private const string ZipUrl = "https://github.com/dectalk/dectalk/releases/download/2023-10-30/vs2022.zip";
    // https://github.com/dectalk/dectalk/releases/download/2023-10-30/vs2022.zip
    //   sha256: 4a778056c109b37f95ade4b3d3e308b9396b22a4b0629f9756ec0e5051b9636d
    // AMD64/lib/dtalk_us.dll
    //   sha256: 9ccad42378b01581ad6cd2fdfcf3af565c8d8bb87008d56360a1c67b27029fb1
    // AMD64/dic/dtalk_us.dic
    //   sha256: 3aab048d867585185bbff239181f46342403a1d151942d2d544a42e5b621373c

    public DecTalk(string configPath, AudioPlayer player)
    {
        this.configPath = configPath;
        audioPlayer = player;
        audioPlayer.SetSourceWaveFormat(11025, 1);
        libraryTask = LoadLibraryAsync(configPath);
    }

    private async Task<bool> LoadLibraryAsync(string configPath)
    {
        bool download = false;
        var zipFiles = new[] {
            (path: "AMD64/lib/dtalk_us.dll", hash: "9ccad42378b01581ad6cd2fdfcf3af565c8d8bb87008d56360a1c67b27029fb1"),
            (path: "AMD64/dic/dtalk_us.dic", hash: "3aab048d867585185bbff239181f46342403a1d151942d2d544a42e5b621373c")
        };
        try {
            foreach(var file in zipFiles)
            {
                var fileOnDisk = Path.Join(configPath, "dectalk", Path.GetFileName(file.path));
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
                var resPath = Path.Join(configPath, "dectalk");
                if(!Directory.Exists(resPath)) Directory.CreateDirectory(resPath);
                using var client = new HttpClient();
                using var response = await client.GetAsync(ZipUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                using var responseStream = await response.Content.ReadAsStreamAsync(cts.Token);
                using var zip = new ZipArchive(responseStream);
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
        try {
            Reset(true);
            AssertCall(DecTalkImports.TextToSpeechShutdown(this.handle),"TextToSpeechShutdown");
            DecTalkImports.Free();
        } catch(Exception e) {
            STLog.Log.Error(e, "DecTalk.Dispose(): Exception caught:");
        }
        GC.SuppressFinalize(this);
    }

    public void Speak(string text, bool extra)
    {
        if(!IsInitialized()) return;
        try {
            // If something bad happens here... pray
            unsafe {
                var syncText = text + " [:sync]";
                // For some reason punctuation locks it up, needs deeper testing
                syncText = syncText.Replace(".", "");
                syncText = syncText.Replace(",", "");
                syncText = syncText.Replace("!", "");
                syncText = syncText.Replace("?", "");

                var buffSize = 1024 * 1024 * 2; // 2MB
                var buffer = new DecTalkImports.TTS_BUFFER();
                buffer.Data = Marshal.AllocHGlobal(buffSize);
                buffer.MaximumBufferLength = buffSize;
                AssertCall(DecTalkImports.TextToSpeechAddBuffer(this.handle, &buffer), "TextToSpeechAddBuffer");
                AssertCall(DecTalkImports.TextToSpeechSpeak(this.handle, syncText, DecTalkImports.SpeechFlags.Force), "TextToSpeechSpeak");
                AssertCall(DecTalkImports.TextToSpeechSync(this.handle), "TextToSpeechSync");

                var data = new byte[buffSize];
                Marshal.Copy(buffer.Data, data, 0, buffSize);
                audioPlayer.Enqueue(data);

                Marshal.FreeHGlobal(buffer.Data);
                Reset(false);
            }
        } catch (Exception e) {
            STLog.Log.Error(e, "DecTalk.Speak(): Exception caught:");
        }
    }

    public void SetVoice(string voice)
    {
        if(!IsInitialized()) return;
        if(!Enum.TryParse(voice, true, out DecTalkVoice dv))
        {
            STLog.Log.Warning($"Could not find voice {voice}. Falling back to default.");
            dv = DecTalkVoice.PAUL;
        }
        try {
            AssertCall(DecTalkImports.TextToSpeechSetSpeaker(this.handle, dv), "TextToSpeechSetSpeaker");
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
        if(!IsInitialized()) return;
        try {
            AssertCall(DecTalkImports.TextToSpeechSetRate(this.handle, (uint)speed), "TextToSpeechSetRate");
        } catch (Exception e) {
            STLog.Log.Error(e, "Exception caught:");
        }
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

    private bool TryInitLibrary()
    {
        if(this.handle == IntPtr.Zero)
        {
            try {
                DecTalkImports.SetupResolver(Path.Join(configPath, "dectalk/dtalk_us.dll"));
                var dictionaryPath = Path.Join(configPath, "dectalk/dtalk_us.dic");
                AssertCall(
                    DecTalkImports.TextToSpeechStartupExFonix(ref this.handle, -1, 0, nint.Zero, 0, dictionaryPath),
                    "TextToSpeechStartup");
                AssertCall(
                    DecTalkImports.TextToSpeechOpenInMemory(this.handle, DecTalkImports.WaveFormat.WAVE_FORMAT_1M16),
                    "TextToSpeechOpenInMemory");
            } catch (Exception e) {
                STLog.Log.Error(e, "DecTalk.LoadLibraryAsync(): Exception caught:");
                this.handle = IntPtr.Zero;
            }
        }
        return this.handle != IntPtr.Zero;
    }

    public void Reset(bool reset)
    {
        if(!IsInitialized()) return;
        AssertCall(DecTalkImports.TextToSpeechReset(this.handle, reset), "TextToSpeechReset");
    }

    public void GetStatus(out uint[] statuses)
    {
        statuses = [0, 0, 0];
        if(!IsInitialized()) return;

        DecTalkImports.StatusId[] identifiers = [
            DecTalkImports.StatusId.INPUT_CHARACTER_COUNT,
            DecTalkImports.StatusId.STATUS_SPEAKING,
            DecTalkImports.StatusId.WAVE_OUT_DEVICE_ID
        ];
        AssertCall(
            DecTalkImports.TextToSpeechGetStatus(this.handle, identifiers, statuses, 3),
            "TextToSpeechGetStatus"
        );
    }

    private static void AssertCall(uint value, string method) {
        if (value != 0) throw new Exception($"Calling {method} returned error code {value}");
    }
}