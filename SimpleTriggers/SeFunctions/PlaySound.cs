// (c) 2025 Ottermandias (ChatAlerts)
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SimpleTriggers.SeFunctions
{
    public sealed class PlaySound
    {
        public static void Play(Sounds id) {
            UIGlobals.PlaySoundEffect((uint)id);
        }
    }
}
