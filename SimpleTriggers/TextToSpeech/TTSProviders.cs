namespace SimpleTriggers.TextToSpeech;

public enum TextToSpeechType : byte
{
    None = 0,
    Kokoro,
    WindowsSystem,
#if DEBUG
    DecTalk,
#endif
    //eSpeakNG,
    //flite
}

public static class TTSProviders
{
    public static string ToName(TextToSpeechType ttst)
    {
        return ttst switch
        {
            TextToSpeechType.None           => "None",
            TextToSpeechType.Kokoro         => "Kokoro",
            TextToSpeechType.WindowsSystem  => "Windows System",
        #if DEBUG
            TextToSpeechType.DecTalk        => "DECtalk",
        #endif
            //TextToSpeechType.eSpeakNG       => "espeak-ng",
            //TextToSpeechType.flite          => "flite",
            _                               => ""
        };
    }
}