namespace SimpleTriggers.TextToSpeech;

public enum TextToSpeechType : byte
{
    None = 0,
    Kokoro,
    WindowsSystem,
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
            //TextToSpeechType.eSpeakNG       => "espeak-ng",
            //TextToSpeechType.flite          => "flite",
            _                               => ""
        };
    }
}