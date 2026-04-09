using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SimpleTriggers.TextToSpeech;

public class AudioPlayer : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly WaveFormat waveFormat = new (24000, 16, 1);
    private readonly ConcurrentQueue<byte[]> queue = [];
    private WaveOutEvent waveOut;
    volatile float volume = 1.0f;
    volatile bool hasExited = false;

    public AudioPlayer(string deviceId = "")
    {
        waveOut = new WaveOutEvent() { DeviceNumber = DeviceIdToNumber(deviceId) };
        new Thread(async() => {
            while(!hasExited) {
                await Task.Delay(50);
                // check queue
                while(!hasExited && queue.TryDequeue(out var packet))
                {
                    await semaphore.WaitAsync();
                    try {
                        var stream = new RawSourceWaveStream(packet, 0, packet.Length, waveFormat);
                        var mix = new VolumeSampleProvider(stream.ToSampleProvider()) { Volume = volume };
                        waveOut.Init(mix);
                        waveOut.Play();
                        while(!hasExited && waveOut.PlaybackState == PlaybackState.Playing) { await Task.Delay(5); }
                        if(!hasExited) { waveOut.Stop(); }
                    } finally { semaphore.Release(); }
                }
            }
        }){ IsBackground = true }.Start();
    }

    private int DeviceIdToNumber(string id)
    {
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        for(int i = 0; i < devices.Count-1; ++i)
        {
            if(devices[i].ID.Equals(id)) return i;
        }
        return -1;
    }

    public void SetOutputDevice(int deviceId = 0)
    {
        semaphore.WaitAsync();
        try
        {
            waveOut.Stop();
            waveOut.Dispose();
            waveOut = new WaveOutEvent() { DeviceNumber = deviceId };
        } finally { semaphore.Release(); }
    }

    public void SetOutputDevice(string deviceId)
    {
        SetOutputDevice(DeviceIdToNumber(deviceId));
    }
    
    public void StopPlayback(bool clearQueue = false)
    {
        waveOut.Stop();
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
        waveOut.Stop();
        waveOut.Dispose();
        queue.Clear();
    }

    public void Enqueue(byte[] stream)
    {
        ObjectDisposedException.ThrowIf(hasExited, this);
        queue.Enqueue(stream);
    }
    
}