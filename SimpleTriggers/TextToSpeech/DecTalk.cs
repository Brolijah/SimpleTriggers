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
    private readonly DecTalkImports.CallbackDelegate callbackRef;
    private readonly string configPath;
    private const string ZipUrl = "https://github.com/dectalk/dectalk/releases/download/2023-10-30/vs2022.zip";
    // https://github.com/dectalk/dectalk/releases/download/2023-10-30/vs2022.zip
    //   sha256: 4a778056c109b37f95ade4b3d3e308b9396b22a4b0629f9756ec0e5051b9636d
    // AMD64/lib/dtalk_us.dll
    //   sha256: 9ccad42378b01581ad6cd2fdfcf3af565c8d8bb87008d56360a1c67b27029fb1
    // AMD64/dic/dtalk_us.dic
    //   sha256: 3aab048d867585185bbff239181f46342403a1d151942d2d544a42e5b621373c
    private DecTalkVoice voice = DecTalkVoice.PAUL;
    private uint speed = 200;

    public DecTalk(string configPath, AudioPlayer player)
    {
        this.configPath = configPath;
        audioPlayer = player;
        audioPlayer.SetSourceWaveFormat(11025, 1);
        libraryTask = LoadLibraryAsync(configPath);
        callbackRef = Callback;
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
                var zipData = await client.GetByteArrayAsync(ZipUrl);
                if(!(Convert.ToHexStringLower(SHA256.HashData(zipData)) == "4a778056c109b37f95ade4b3d3e308b9396b22a4b0629f9756ec0e5051b9636d"))
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
        if(this.handle != IntPtr.Zero)
        {
            // This is *purposefully* bad code. Until I can figure out why dectalk locks *randomly*,
            // this will at the very least not block the main thread.
            Task.Run(() => {
                try {
                    Reset(true);
                    AssertCall(DecTalkImports.TextToSpeechShutdown(this.handle),"TextToSpeechShutdown");
                    DecTalkImports.Free();
                } catch(Exception e) {
                    STLog.Log.Error(e, "DecTalk.Dispose(): Exception caught:");
                }
            });
        }
        cts.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Speak(string text, bool extra)
    {
        if(!IsInitialized()) return;
        //STLog.Log.Warning("Entering DetTalk.Speak()");
        byte[] data = [];
        string[] punctuation = [".", ",", "!", "?", ";", ": "];
        var clauses = text.Split(punctuation, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        try {
            foreach(var clause in clauses)
            {
                cts.Token.ThrowIfCancellationRequested();
                // If something bad happens here... pray
                unsafe {
                    var buffSize = 1024 * 1024; // 1 MB ~45 seconds?
                    var buffer = new DecTalkImports.TTS_BUFFER();
                    buffer.Data = Marshal.AllocHGlobal(buffSize);
                    buffer.MaximumBufferLength = buffSize;
                    AssertCall(DecTalkImports.TextToSpeechAddBuffer(this.handle, &buffer), "TextToSpeechAddBuffer", cts.Token);
                    AssertCall(DecTalkImports.TextToSpeechSpeak(this.handle, clause, DecTalkImports.SpeechFlags.Force), "TextToSpeechSpeak", cts.Token);
                    // If Sync hangs, this gets captured by the timeoutTask in ProcessSpeakQueue
                    // it ideally shouldn't, but we have no idea how long it can hang for.
                    AssertCall(DecTalkImports.TextToSpeechSync(this.handle), "TextToSpeechSync", cts.Token);
                    cts.Token.ThrowIfCancellationRequested();

                    var chunk = new byte[buffSize];
                    Marshal.Copy(buffer.Data, chunk, 0, buffSize);
                    data = [.. data, .. TrimAudioBuffer(chunk)];
                    Marshal.FreeHGlobal(buffer.Data);
                    Reset(true);
                }
            }
            cts.Token.ThrowIfCancellationRequested();
            audioPlayer.Enqueue(data); // trims just the end of the buffer
        } catch (Exception e) {
            STLog.Log.Error(e, "DecTalk.Speak(): Exception caught:");
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
        if(!Enum.TryParse(voice, true, out DecTalkVoice dv))
        {
            STLog.Log.Warning($"Could not find voice {voice}. Falling back to default.");
            dv = DecTalkVoice.PAUL;
        }
        this.voice = dv;
        if(!IsInitialized()) return;
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
        this.speed = (uint)speed;
        if(!IsInitialized()) return;
        try {
            AssertCall(DecTalkImports.TextToSpeechSetRate(this.handle, this.speed), "TextToSpeechSetRate");
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
                    DecTalkImports.TextToSpeechStartupExFonix(ref this.handle, -1, 0, Marshal.GetFunctionPointerForDelegate(callbackRef), 0, dictionaryPath),
                    "TextToSpeechStartup");
                AssertCall(
                    DecTalkImports.TextToSpeechOpenInMemory(this.handle, DtWaveFormat.WAVE_FORMAT_1M16),
                    "TextToSpeechOpenInMemory");
                // workaround for a race condition where these may be set before the library is loaded
                AssertCall(DecTalkImports.TextToSpeechSetSpeaker(this.handle, this.voice), "TextToSpeechSetSpeaker");
                AssertCall(DecTalkImports.TextToSpeechSetRate(this.handle, this.speed), "TextToSpeechSetRate");
            } catch (Exception e) {
                STLog.Log.Error(e, "DecTalk.LoadLibraryAsync(): Exception caught:");
                this.handle = IntPtr.Zero;
            }
        }
        return this.handle != IntPtr.Zero;
    }

    private void Callback(long param1, long param2, uint cbParameter, uint uiMsg)
    {
        /*
        var msgFlag = (DtCallbackId)(uiMsg&0xF); // GIBBERISH?????
        STLog.Log.Warning(
            $"DecTalk.Callback():\n"+
            $"  param1  = {(DtError)param1}\n"+
            $"  cbParam = {cbParameter}\n"+
            $"  uiMsg   = {msgFlag}");
        
        // If something wrong happens here it's prboably dunzo
        /* try { unsafe {
            //if(msgFlag == DtCallbackId.MGS_BUFFER)
            //{
                var buffer = (DecTalkImports.TTS_BUFFER*)param2;
                STLog.Log.Warning(
                    $"DecTalk.Callback() : MSG_BUFFER\n"+
                    $"  buffer->BufferLength           = {buffer->BufferLength}\n"+
                    $"  buffer->MaximumBufferLength    = {buffer->MaximumBufferLength}\n"+
                    $"  buffer->NumberOfPhonemeChanges = {buffer->NumberOfPhonemeChanges}\n"+
                    $"  buffer->NumberOfIndexMarks     = {buffer->NumberOfIndexMarks}");
                //Reset(false);
            //}
        }} catch (Exception e)
        {
            STLog.Log.Error(e, "Exception caught:");
        } */
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

        DtStatusId[] identifiers = [
            DtStatusId.INPUT_CHARACTER_COUNT,
            DtStatusId.STATUS_SPEAKING,
            DtStatusId.WAVE_OUT_DEVICE_ID
        ];
        AssertCall(
            DecTalkImports.TextToSpeechGetStatus(this.handle, identifiers, statuses, 3),
            "TextToSpeechGetStatus"
        );
    }

    private static void AssertCall(uint value, string method, CancellationToken? token = null) {
        token?.ThrowIfCancellationRequested();
        if (value != 0) throw new Exception($"Calling {method} returned error code {(DtError)value}");
    }
}