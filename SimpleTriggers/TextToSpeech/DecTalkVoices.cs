using System;

namespace SimpleTriggers.TextToSpeech;

public enum DecTalkVoice : uint
{
    PAUL = 0,
	BETTY = 1,
	HARRY = 2,
	FRANK = 3,
	DENNIS = 4,
	KIT = 5,
	URSULA = 6,
	RITA = 7,
	WENDY = 8,
}

public static class DecTalkVoiceHelper
{
	public static string ToString(DecTalkVoice voice)
	{
		return Enum.GetName(voice) ?? $"Who? {{{voice}}}";
	}
}