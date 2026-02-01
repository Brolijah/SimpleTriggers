namespace SimpleTriggers.TextToSpeech;

public enum TextToSpeechType : byte
{
    None = 0,
    eSpeakNG,
    flite,
    Kokoro
}

public static class TTSProviders
{
    public static string ToName(TextToSpeechType ttst)
    {
        return ttst switch
        {
            TextToSpeechType.None     => "None",
            TextToSpeechType.eSpeakNG => "espeak-ng",
            TextToSpeechType.flite    => "flite",
            TextToSpeechType.Kokoro   => "Kokoro",
            _                         => ""
        };
    }
}