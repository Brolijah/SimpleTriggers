using System;

namespace SimpleTriggers.TextToSpeech;

public enum DtError : uint
{
	NOERROR     ,    /* no error */
	ERROR       ,    /* unspecified error */
	RESET       ,    /* Start returned early due to reset */
	INDEX       ,    /* Data in callback is index */
}

public enum DtWaveFormat : uint {
	WAVE_INVALIDFORMAT  =  0,       /* invalid format */
	WAVE_FORMAT_1M16    =  1,       /* 11.025 kHz, Mono,   16-bit */
	WAVE_FORMAT_08M16   =  2,       /* 8      kHz, Mono,   16-bit */
}

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

	public static string ToMiniString(DecTalkVoice voice)
	{
		return voice switch
		{
			DecTalkVoice.PAUL   => "np",
			DecTalkVoice.BETTY  => "nb",
			DecTalkVoice.HARRY  => "nh",
			DecTalkVoice.FRANK  => "nf",
			DecTalkVoice.DENNIS => "nd",
			DecTalkVoice.KIT    => "nk",
			DecTalkVoice.URSULA => "nu",
			DecTalkVoice.RITA   => "nr",
			DecTalkVoice.WENDY  => "nw",
			_                   => "np" // default to paul
		};
	}
}
