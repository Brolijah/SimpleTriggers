using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Utility;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SimpleTriggers.Logger;

namespace SimpleTriggers.TextToSpeech;

public enum AudioOutputType
{
    WaveOut, // Can only support default audio device
    DirectSound,
    Wasapi,
}

public class AudioPlayer : IDisposable
{
    private readonly ConcurrentQueue<byte[]> queue = [];
    private string deviceGuid;
    private AudioOutputType audioBackend;
    private IWavePlayer? wavePlayer;
    private readonly MixingSampleProvider mixer;
    private volatile float volume = 1.0f;
    private volatile bool hasExited = false;

    public AudioPlayer(string deviceId = "", AudioOutputType backend = AudioOutputType.DirectSound)
    {
        deviceGuid = deviceId;
        audioBackend = backend;
        mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)) { ReadFully = true };
        InitializeAudioBackend(backend, deviceGuid);

        new Thread(async() => {
            while(!hasExited) {
                await Task.Delay(50);
                // check queue
                while(!hasExited && queue.TryDequeue(out var packet))
                {
                    try {
                        using var stream = new RawSourceWaveStream(packet, 0, packet.Length, new (24000, 16, 1));
                        var vmix = new VolumeSampleProvider(stream.ToSampleProvider()) { Volume = volume };
                        var smix = new WdlResamplingSampleProvider(vmix, mixer.WaveFormat.SampleRate);
                        mixer.AddMixerInput(smix);
                        //await Task.Delay(stream.TotalTime); // prevents streams from overlapping (it's funnier not to)
                    } catch (Exception e)
                    { STLog.Log.Error(e, "Exception caught:"); }
                }
            }
        }){ IsBackground = true }.Start();
    }

    public void InitializeAudioBackend(AudioOutputType type, string? deviceId)
    {
        audioBackend = type;
        deviceGuid = deviceId ?? deviceGuid;
        wavePlayer?.Stop();
        wavePlayer?.Dispose();
        wavePlayer = null;
        try {
            wavePlayer = audioBackend switch
            {
                AudioOutputType.WaveOut => new WaveOutEvent(),
                AudioOutputType.DirectSound when deviceGuid.IsNullOrEmpty() => new DirectSoundOut(),
                AudioOutputType.DirectSound => new DirectSoundOut(new Guid(deviceGuid)),
                AudioOutputType.Wasapi when deviceGuid.IsNullOrEmpty() => new WasapiOut(AudioClientShareMode.Shared, true, 100),
                AudioOutputType.Wasapi => new WasapiOut(GetWasapiAudioDevice(deviceGuid), AudioClientShareMode.Shared, true, 100),
                _=> throw new ArgumentOutOfRangeException()
            };
            STLog.Log.Info($"Setting audio backend {{{audioBackend}}}");
        } catch (Exception e)
        {
            STLog.Log.Error($"Failed to initialize the audio backend with type \"{type}\" and GUID \"{deviceGuid}\"");
            STLog.Log.Error(e, "Exception caught:");
            STLog.Log.Warning("Falling back to WaveOutEvent");
            wavePlayer = new WaveOutEvent(); // uhh...
        }
        wavePlayer.Init(mixer);
        wavePlayer.Play();
    }

    public void SetOutputDevice(string deviceId)
    {
        STLog.Log.Info($"Setting output device {deviceId}");
        InitializeAudioBackend(audioBackend, deviceId);
    }

    // Warning: Can throw, only call inside a try-catch block
    private MMDevice GetWasapiAudioDevice(string deviceId)
    {
        using var enumerator = new MMDeviceEnumerator();
        foreach(var d in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if(d.ID.Contains(deviceId, StringComparison.CurrentCultureIgnoreCase))
            {
                return d;
            }
        }
        STLog.Log.Warning($"Device ID does not exist: \"{deviceId}\"");
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
    }
    
    public void StopPlayback(bool clearQueue = false)
    {
        if(clearQueue)
        {
            queue.Clear();
        }
    }

    // volume = [0.0f, 100.0f] // technically allows values >100, and that *might* be fine?
    public void SetVolume(float volume)
    {
        this.volume = volume/100f;
    }

    public void Dispose()
    {
        hasExited = true;
        wavePlayer?.Stop();
        wavePlayer?.Dispose();
        queue.Clear();
    }

    public void Enqueue(byte[] stream)
    {
        ObjectDisposedException.ThrowIf(hasExited, this);
        queue.Enqueue(stream);
    }
    
}