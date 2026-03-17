using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SimpleTriggers.TextToSpeech;

// This just re-implements most of KokoroPlayback, but adds VolumeSampleProvider
// if KokoroSharp is updated to include VolumeSampleProvider, I'll just discard this
public class AudioPlayer : IDisposable
{
    private readonly WaveFormat waveFormat = new (24000, 16, 1);
    private readonly WaveOutEvent waveOut;
    private readonly ConcurrentQueue<float[]> queue;
    volatile float volume = 1.0f;
    volatile bool hasExited = false;
    
    public AudioPlayer()
    {
        waveOut = new WaveOutEvent();
        queue = [];
        
        new Thread(async() => {
            while(!hasExited) {
                await Task.Delay(50);
                // check queue
                while(!hasExited && queue.TryDequeue(out var packet))
                {
                    var stream = new RawSourceWaveStream(GetBytes(packet), 0, packet.Length*2, waveFormat);
                    var mix = new VolumeSampleProvider(stream.ToSampleProvider());
                    mix.Volume = this.volume;
                    waveOut.Init(mix);
                    waveOut.Play();
                    while(!hasExited && waveOut.PlaybackState == PlaybackState.Playing) { await Task.Delay(5); }
                    if(!hasExited) { waveOut.Stop(); }
                }
            }
        }){ IsBackground = true }.Start();
    }
    
    public void StopPlayback(bool clearQueue = false)
    {
        waveOut.Stop();
        if(clearQueue)
        {
            queue.Clear();
        }
    }
    // volume = [0.0f, 100.0f] // technically allows values >100, and that *might* be fine? idk
    public void SetVolume(float volume)
    {
        this.volume = volume;
    }

    public void Enqueue(float[] samples)
    {
        ObjectDisposedException.ThrowIf(hasExited, this);
        queue.Enqueue(samples);
    }
    
    public void Dispose()
    {
        hasExited = true;
        waveOut.Stop();
        waveOut.Dispose();
        queue.Clear();
    }

    public byte[] GetBytes(float[] samples)
    {
        return samples.Select((float f) => (short)(f * 32767f)).SelectMany(BitConverter.GetBytes).ToArray();
    }
}