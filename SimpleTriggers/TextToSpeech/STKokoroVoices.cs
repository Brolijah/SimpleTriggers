using System;

namespace SimpleTriggers.TextToSpeech;

// For more details about the different voices, see this page:
// https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md

public enum KokoroVoiceKind
{
    // American English    
    af_heart = 0,
    af_bella,
    af_alloy,
    af_aoede,
    af_jessica,
    af_kore,
    af_nicole,
    af_nova,
    af_river,
    af_sarah,
    af_sky,
    am_adam,
    am_echo,
    am_eric,
    am_fenrir,
    am_liam,
    am_michael,
    am_onyx,
    am_puck,
    am_santa,
    // British English
    bf_alice,
    bf_emma,
    bf_isabella,
    bf_lily,
    bm_daniel,
    bm_fable,
    bm_george,
    bm_lewis,
    // Spanish
    ef_dora,
    em_alex,
    em_santa,
    // Italian
    if_sara,
    im_nicola,
    // Brazillian Portuguese
    pf_dora,
    pm_alex,
    pm_santa,
    // French
    ff_siwis,
/*
    // Hindi
    hf_alpha,
    hf_beta,
    hm_omega,
    hm_psi,
    // Japanese
    jf_alpha,
    jf_gongitsune,
    jf_nezumi,
    jf_tebukuro,
    jm_kumo,
    // Mandarin Chinese
    zf_xiaobei,
    zf_xiaoni,
    zf_xiaoxiao,
    zf_xiaoyi
*/
}

public static class KokoroVoiceHelper
{
    public static string ToName(KokoroVoiceKind voice)
    {
        return voice switch
        {
            // American English    
            KokoroVoiceKind.af_heart => "Heart (F) (EN-US)",
            KokoroVoiceKind.af_bella => "Bella (F) (EN-US)",
            KokoroVoiceKind.af_alloy => "Alloy (F) (EN-US)",
            KokoroVoiceKind.af_aoede => "Aoede(F) (EN-US)",
            KokoroVoiceKind.af_jessica => "Jessica (F) (EN-US)",
            KokoroVoiceKind.af_kore => "Kore (F) (EN-US)",
            KokoroVoiceKind.af_nicole => "Nicole (F) (EN-US)",
            KokoroVoiceKind.af_nova => "Nova (F) (EN-US)",
            KokoroVoiceKind.af_river => "River (F) (EN-US)",
            KokoroVoiceKind.af_sarah => "Sarah (F) (EN-US)",
            KokoroVoiceKind.af_sky => "Sky (F) (EN-US)",
            KokoroVoiceKind.am_adam => "Adam (M) (EN-US)",
            KokoroVoiceKind.am_echo => "Echo (M) (EN-US)",
            KokoroVoiceKind.am_eric => "Eric (M) (EN-US)",
            KokoroVoiceKind.am_fenrir => "Fenrir (M) (EN-US)",
            KokoroVoiceKind.am_liam => "Liam (M) (EN-US)",
            KokoroVoiceKind.am_michael => "Michael (M) (EN-US)",
            KokoroVoiceKind.am_onyx => "Onyx (M) (EN-US)",
            KokoroVoiceKind.am_puck => "Puck (M) (EN-US)",
            KokoroVoiceKind.am_santa => "Santa (M) (EN-US)",
            // British English
            KokoroVoiceKind.bf_alice => "Alice (F) (EN-UK)",
            KokoroVoiceKind.bf_emma => "Emma (F) (EN-UK)",
            KokoroVoiceKind.bf_isabella => "Isabella (F) (EN-UK)",
            KokoroVoiceKind.bf_lily => "Lily (F) (EN-UK)",
            KokoroVoiceKind.bm_daniel => "Daniel (M) (EN-UK)",
            KokoroVoiceKind.bm_fable => "Fable (M) (EN-UK)",
            KokoroVoiceKind.bm_george => "George (M) (EN-UK)",
            KokoroVoiceKind.bm_lewis => "Lewis (M) (EN-UK)",
            // Spanish
            KokoroVoiceKind.ef_dora => "Dora (F) (ES)",
            KokoroVoiceKind.em_alex => "Alex (M) (ES)",
            KokoroVoiceKind.em_santa => "Santa (M) (ES)",
            // Italian
            KokoroVoiceKind.if_sara => "Sara (F) (IT)",
            KokoroVoiceKind.im_nicola => "Nicola (M) (IT)",
            // Brazillian Portuguese
            KokoroVoiceKind.pf_dora => "Dora (F) (PT)",
            KokoroVoiceKind.pm_alex => "Alex (M) (PT)",
            KokoroVoiceKind.pm_santa => "Santa (M) (PT)",
            // French
            KokoroVoiceKind.ff_siwis => "Siwis (F) (FR)",
        /*
            // Hindi
            KokoroVoiceKind.hf_alpha => "Alpha (F) (HI)",
            KokoroVoiceKind.hf_beta => "Beta (F) (HI)",
            KokoroVoiceKind.hm_omega => "Omega (M) (HI)",
            KokoroVoiceKind.hm_psi => "Psi (M) (HI)",
            // Japanese
            KokoroVoiceKind.jf_alpha => "Alpha (F) (JP)",
            KokoroVoiceKind.jf_gongitsune => "Gongitsune (F) (JP)",
            KokoroVoiceKind.jf_nezumi => "Nezumi (F) (JP)",
            KokoroVoiceKind.jf_tebukuro => "Tebukuro (F) (JP)",
            KokoroVoiceKind.jm_kumo => "Kumo (M) (JP)",
            // Mandarin Chinese
            KokoroVoiceKind.zf_xiaobei => "Xiaobei (F) (CH)",
            KokoroVoiceKind.zf_xiaoni => "Xiaoni (F) (CH)",
            KokoroVoiceKind.zf_xiaoxiao => "Xiaoxiao (F) (CH)",
            KokoroVoiceKind.zf_xiaoyi => "Xiaoyi (F) (CH)",
        */
            _ => "UNKNOWN"
        };
    }

    public static string ToString(KokoroVoiceKind voiceId)
    {
        return Enum.GetName(typeof(KokoroVoiceKind), voiceId) ?? "af_heart";
    }
}