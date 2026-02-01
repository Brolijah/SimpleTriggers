using System.IO;
using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;

namespace SimpleTriggers.TextToSpeech;

public class ST_Kokoro
{
    private string assemblyPath {get;init;}
    private KokoroTTS tts {get;init;}
    private KokoroVoice kv {get;init;}
    public ST_Kokoro(string path)
    {
        assemblyPath = path;
        tts = KokoroTTS.LoadModel(Path.Join(assemblyPath, "kokoro-quant.onnx"));
        Tokenizer.eSpeakNGPath = Path.Join(assemblyPath, "espeak");
        KokoroVoiceManager.LoadVoicesFromPath(Path.Join(assemblyPath,"voices"));
        kv = KokoroVoiceManager.GetVoice("af_bella");
    }

    public void Speak(string message)
    {
        tts.StopPlayback();
        tts.SpeakFast(message, kv);
        
    }
}
