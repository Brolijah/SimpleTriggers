namespace SimpleTriggers.TextToSpeech;

public enum TextToSpeechType : byte
{
    None = 0,
    Kokoro,
    WindowsSystem,
    DecTalk,
    eSpeakNG = 98,
    //Custom = 99
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
            TextToSpeechType.DecTalk        => "DECtalkMini",
            TextToSpeechType.eSpeakNG       => "espeak-ng",
            //TextToSpeechType.Custom       => "Custom",
            _                               => ""
        };
    }
}