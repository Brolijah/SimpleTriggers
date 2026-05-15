namespace SimpleTriggers.TextToSpeech;

public enum TextToSpeechType : byte
{
    None = 0,
    Kokoro,
    WindowsSystem,
    DecTalk,
    //eSpeakNG,
    //flite,
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
            //TextToSpeechType.eSpeakNG       => "espeak-ng",
            //TextToSpeechType.flite          => "flite",
            //TextToSpeechType.Custom         => "Custom",
            _                               => ""
        };
    }
}