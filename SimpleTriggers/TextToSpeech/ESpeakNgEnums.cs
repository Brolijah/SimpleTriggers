
using System;

namespace SimpleTriggers.TextToSpeech;

public enum EsPosition
{
    POS_CHARACTER = 1,
    POS_WORD,
    POS_SENTENCE
}

public enum EsError
{
    EE_OK=0,
	EE_INTERNAL_ERROR=-1,
	EE_BUFFER_FULL=1,
	EE_NOT_FOUND=2
}

public enum EsPhoneme
{
    INIT_PHONEME_EVENTS = 0x0001,
    INIT_PHONEME_IPA    = 0x0002,
    INIT_DONT_EXIT      = 0x8000
}

public enum EsAudioOutput
{
    /* PLAYBACK mode: plays the audio data, supplies events to the calling program*/
	AUDIO_OUTPUT_PLAYBACK,

	/* RETRIEVAL mode: supplies audio data and events to the calling program */
	AUDIO_OUTPUT_RETRIEVAL,

	/* SYNCHRONOUS mode: as RETRIEVAL but doesn't return until synthesis is completed */
	AUDIO_OUTPUT_SYNCHRONOUS,

	/* Synchronous playback */
	AUDIO_OUTPUT_SYNCH_PLAYBACK
}

[Flags]
public enum EsCharMode : int
{
    espeakCHARS_AUTO = 0,   // 8 bit or UTF8 (this is the default)
    espeakCHARS_UTF8,       // UTF8 encoding
    espeakCHARS_8BIT,       // The 8 bit ISO-8859 character set for the particular language.
    espeakCHARS_WCHAR,      // Wide characters (wchar_t)
    espeakCHARS_16BIT       // 16 bit characters.
}

public enum EsParameter
{
    espeakSILENCE=0, /* internal use */
    espeakRATE=1,
    espeakVOLUME=2,
    espeakPITCH=3,
    espeakRANGE=4,
    espeakPUNCTUATION=5,
    espeakCAPITALS=6,
    espeakWORDGAP=7,
    espeakOPTIONS=8,   // reserved for misc. options.  not yet used
    espeakINTONATION=9,
    espeakSSML_BREAK_MUL=10,

    espeakRESERVED2=11,
    espeakEMPHASIS,   /* internal use */
    espeakLINELENGTH, /* internal use */
    espeakVOICETYPE,  // internal, 1=mbrola
    N_SPEECH_PARAM    /* last enum */
}

public enum EsPuncType
{
    espeakPUNCT_NONE=0,
    espeakPUNCT_ALL=1,
    espeakPUNCT_SOME=2
}

/* Unsure where I'll need this yet
public enum EsNgStatus
{
    ENS_GROUP_MASK               = 0x70000000,
	ENS_GROUP_ERRNO              = 0x00000000, // Values 0-255 map to errno error codes.
	ENS_GROUP_ESPEAK_NG          = 0x10000000, // eSpeak NG error codes.

	// eSpeak NG 1.49.0
	ENS_OK                       = 0,
	ENS_COMPILE_ERROR            = 0x100001FF,
	ENS_VERSION_MISMATCH         = 0x100002FF,
	ENS_FIFO_BUFFER_FULL         = 0x100003FF,
	ENS_NOT_INITIALIZED          = 0x100004FF,
	ENS_AUDIO_ERROR              = 0x100005FF,
	ENS_VOICE_NOT_FOUND          = 0x100006FF,
	ENS_MBROLA_NOT_FOUND         = 0x100007FF,
	ENS_MBROLA_VOICE_NOT_FOUND   = 0x100008FF,
	ENS_EVENT_BUFFER_FULL        = 0x100009FF,
	ENS_NOT_SUPPORTED            = 0x10000AFF,
	ENS_UNSUPPORTED_PHON_FORMAT  = 0x10000BFF,
	ENS_NO_SPECT_FRAMES          = 0x10000CFF,
	ENS_EMPTY_PHONEME_MANIFEST   = 0x10000DFF,
	ENS_SPEECH_STOPPED           = 0x10000EFF,

	// eSpeak NG 1.49.2
	ENS_UNKNOWN_PHONEME_FEATURE  = 0x10000FFF,
	ENS_UNKNOWN_TEXT_ENCODING    = 0x100010FF,
	ENS_UNEXPECTED_EOF           = 0x100011FF,
} */
