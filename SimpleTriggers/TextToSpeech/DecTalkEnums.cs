using System;

namespace SimpleTriggers.TextToSpeech;

#if DEBUG
// https://github.com/dectalk/dectalk/blob/32efa30ef2e216b3ad091c41abf5b502498a19aa/src/dapi/src/api/ttsapi.h

public enum DtError : uint
{
	NOERROR     ,    /* no error */
	ERROR       ,    /* unspecified error */
	BADDEVICEID ,    /* device ID out of range */
	NOTENABLED  ,    /* driver failed enable */
	ALLOCATED   ,    /* device already allocated */
	INVALHANDLE ,    /* device handle is invalid */
	NODRIVER    ,    /* no device driver present */
	NOMEM       ,    /* memory allocation error */
	NOTSUPPORTED,    /* function isn't supported */
	BADERRNUM   ,    /* error value out of range */
	INVALFLAG   ,    /* invalid flag passed */
	INVALPARAM  ,    /* invalid parameter passed */
	HANDLEBUSY  ,    /* handle being used simultaneously on another thread (eg callback) */
	INVALIDALIAS,    /* "Specified alias not found in WIN.INI */
}

public enum DtCallbackId : uint
{
	MSG_BUFFER = 9,
	MSG_INDEX_MARK = 1,
	MSG_STATUS = 2,
	MSG_VISUAL = 3
}

[Flags]
public enum DtDeviceOptions : uint
{
	OWN_AUDIO_DEVICE        = 0x00000001,
	REPORT_OPEN_ERROR       = 0x00000002,
	USE_SAPI5_AUDIO_DEVICE  = 0x40000000,
	DO_NOT_USE_AUDIO_DEVICE = 0x80000000
}

[Flags]
public enum DtSpeechFlags : uint {
	Normal = 0,
	Force = 1
}

public enum DtStatusId : uint
{
	INPUT_CHARACTER_COUNT = 0,
	STATUS_SPEAKING = 1,
	WAVE_OUT_DEVICE_ID = 2,
}

public enum DtWaveFormat : uint {
	WAVE_INVALIDFORMAT  =  0x00000000,       /* invalid format */
	WAVE_FORMAT_1M08    =  0x00000001,       /* 11.025 kHz, Mono,   8-bit */
	WAVE_FORMAT_1M16    =  0x00000004,       /* 11.025 kHz, Mono,   16-bit */
	WAVE_FORMAT_08M08   =  0x00001000,       /* 8      kHz, Mono,   8-bit */
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
}
#endif