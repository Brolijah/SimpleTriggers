using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SimpleTriggers.TextToSpeech;

public class ST_eSpeakNG : IUnixTTSCmd, IDisposable
{
    private readonly string fileName;
    private bool isRunning = false;
    private bool stopSignal = false;
    private Queue<string> msgQueue;
    private Thread thread;

    public ST_eSpeakNG(string path = "espeak-ng")
    {
        fileName = path;
        thread = new Thread(this.Loop);
        msgQueue = new Queue<string>();
    }

    public string FileName
    {
        get { return fileName; }
    }

    private async void Loop()
    {
        Process process = new Process();
        if(!this.isRunning)
        {
            //process.StartInfo.FileName = this.fileName;
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c {this.fileName}";
            //process.StartInfo.Arguments = "\"e-speak started\"";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.StandardInput.AutoFlush = true;
            this.isRunning = true;
            Log.Debug("started espeak thread");
        } else { return; }
        
        do {
            try
            {
                using (var si = process.StandardInput)
                {
                    if(si.BaseStream.CanWrite)
                    {
                        lock(msgQueue)
                        {
                            if(msgQueue.Count > 0)
                            {
                                var msg = msgQueue.Dequeue();
                                Log.Debug($"Pushed message to espeak: {msg}");
                                si.WriteLine(msg);
                            }
                        }
                    } else
                    {
                        Log.Error("Cannot write to stream!!");
                        break;
                    }
                }
            } catch (Exception e)
            {
                Log.Error($"Exception caught: {e.Message}");
                break;
            }
            Thread.Sleep(10); // 10 ms
        } while(!stopSignal && process.Responding);

        isRunning = false;
        stopSignal = false;
        Log.Debug("Reached the end of Loop()");
    }

    private static void ReadStream(StreamReader reader, string streamType)
    {
        while(!reader.EndOfStream)
        {
            Log.Debug($"espeak: {streamType}: {reader.ReadLine()}");
        }
    }


    public bool Start()
    {
        var ret = false;
        if(!isRunning)
        {
            thread = new Thread(this.Loop);
            thread.Start();
            ret = true;
        }
        return ret;
    }

    public void Stop()
    {
        if(isRunning)
        {
            stopSignal = true;
            thread.Join(300);
            stopSignal = false;
            isRunning = false;
            msgQueue.Clear();
        }
        stopSignal = false;
    }
    public void Speak(string message)
    {
        if(isRunning) // only do something if we're running
        {
            lock (msgQueue)
            {
                msgQueue.Enqueue(message);
            }   
        }
    }

    public void Dispose()
    {
        this.Stop();
        msgQueue.Clear();
    }
}
